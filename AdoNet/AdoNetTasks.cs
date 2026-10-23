using DatabaseCourseExam.Data;
using Npgsql;
using NpgsqlTypes;

namespace DatabaseCourseExam.AdoNet;

public static class AdoNetTasks
{
    // Задание 9: IsDBNull перед GetString/GetInt32.
    // Задание 15: параметризованный фильтр по group_id (nullable).
    public static async Task ShowStudentsAsync(int? groupId = null)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        const string sql = """
            SELECT s.id, s.first_name, s.last_name, s.age, g.name
            FROM students AS s
            JOIN student_groups AS g ON g.id = s.group_id
            WHERE (@GroupId IS NULL OR s.group_id = @GroupId)
            ORDER BY s.id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("@GroupId", NpgsqlDbType.Integer).Value =
            (object?)groupId ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);

            string firstName = reader.IsDBNull(1)
                ? "(нет имени)"
                : reader.GetString(1);

            string lastName = reader.IsDBNull(2)
                ? "(нет фамилии)"
                : reader.GetString(2);

            string ageText = reader.IsDBNull(3)
                ? "не указан"
                : reader.GetInt32(3).ToString();

            string groupName = reader.IsDBNull(4)
                ? "—"
                : reader.GetString(4);

            Console.WriteLine(
                $"{id}. {firstName} {lastName}, " +
                $"возраст: {ageText}, группа: {groupName}");
        }
    }

    // Задание 4: валидация возраста до обращения к БД,
    // вывод числа изменённых строк, обработка отсутствия студента.
    public static async Task UpdateStudentAgeAsync(int studentId, int newAge)
    {
        if (newAge is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        const string sql = """
            UPDATE students
            SET age = @Age
            WHERE id = @StudentId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("@Age", NpgsqlDbType.Integer).Value = newAge;
        command.Parameters.Add("@StudentId", NpgsqlDbType.Integer).Value =
            studentId;

        int affectedRows = await command.ExecuteNonQueryAsync();

        Console.WriteLine(
            affectedRows == 0
                ? "Студент не найден."
                : $"Изменено записей: {affectedRows}");
    }

    // Задание 14: транзакция с проверкой существования студента
    // и отсутствия повторной записи; откат при любой ошибке.
    public static async Task EnrollStudentAsync(int studentId, int courseId)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Студент существует?
            await using (var checkStudent = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM students WHERE id = @StudentId);",
                connection, transaction))
            {
                checkStudent.Parameters
                    .Add("@StudentId", NpgsqlDbType.Integer).Value = studentId;

                bool exists = (bool)(await checkStudent.ExecuteScalarAsync())!;
                if (!exists)
                    throw new InvalidOperationException("Студент не найден.");
            }

            // 2. Повторной записи нет?
            await using (var checkDup = new NpgsqlCommand(
                """
                SELECT EXISTS(
                    SELECT 1 FROM student_courses
                    WHERE student_id = @StudentId AND course_id = @CourseId);
                """, connection, transaction))
            {
                checkDup.Parameters
                    .Add("@StudentId", NpgsqlDbType.Integer).Value = studentId;
                checkDup.Parameters
                    .Add("@CourseId", NpgsqlDbType.Integer).Value = courseId;

                bool dup = (bool)(await checkDup.ExecuteScalarAsync())!;
                if (dup)
                    throw new InvalidOperationException(
                        "Студент уже записан на этот курс.");
            }

            // 3. Уменьшаем свободные места.
            await using (var update = new NpgsqlCommand(
                """
                UPDATE courses
                SET available_places = available_places - 1
                WHERE id = @CourseId AND available_places > 0
                RETURNING available_places;
                """, connection, transaction))
            {
                update.Parameters
                    .Add("@CourseId", NpgsqlDbType.Integer).Value = courseId;

                object? result = await update.ExecuteScalarAsync();
                if (result is null)
                    throw new InvalidOperationException(
                        "Курс не найден или свободных мест нет.");
            }

            // 4. INSERT.
            await using (var insert = new NpgsqlCommand(
                """
                INSERT INTO student_courses (student_id, course_id)
                VALUES (@StudentId, @CourseId);
                """, connection, transaction))
            {
                insert.Parameters
                    .Add("@StudentId", NpgsqlDbType.Integer).Value = studentId;
                insert.Parameters
                    .Add("@CourseId", NpgsqlDbType.Integer).Value = courseId;

                await insert.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            Console.WriteLine("Студент записан на курс.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Операция отменена: {ex.Message}");
        }
    }
}