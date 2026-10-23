using DatabaseCourseExam.Data;
using Dapper;
using DatabaseCourseExam.DapperTasks.DTO;

namespace DatabaseCourseExam.DapperTasks;

public static class DapperStudentTasks
{
    // Задание 1: параметризованная фильтрация по группе + проекция в StudentDto.
    // Если groupName == null — фильтр отключён (возвращаются все студенты).
    public static async Task ShowStudentsAsync(string? groupName = null)
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                s.id         AS Id,
                s.first_name AS FirstName,
                s.last_name  AS LastName,
                s.age        AS Age,
                g.name       AS GroupName
            FROM students AS s
            JOIN student_groups AS g ON g.id = s.group_id
            WHERE (@GroupName IS NULL OR g.name = @GroupName)
            ORDER BY s.id;
            """;

        var students = await connection.QueryAsync<StudentDto>(
            sql,
            new { GroupName = groupName });

        foreach (StudentDto student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.GroupName}");
        }
    }

    // Исправлена перепутанная ветка вывода (было: affectedRows > 0 → «не найден»).
    public static async Task UpdateStudentAgeAsync(int studentId, int newAge)
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            UPDATE students
            SET age = @NewAge
            WHERE id = @StudentId;
            """;

        int affectedRows = await connection.ExecuteAsync(
            sql,
            new
            {
                NewAge = newAge,
                StudentId = studentId
            });

        Console.WriteLine(
            affectedRows > 0
                ? $"Изменено записей: {affectedRows}"
                : "Студент не найден.");
    }

    public static async Task ShowCoursesAsync()
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                id               AS Id,
                name             AS Name,
                price            AS Price,
                available_places AS AvailablePlaces
            FROM courses
            ORDER BY name;
            """;

        var courses = await connection.QueryAsync<CourseDto>(sql);

        foreach (CourseDto course in courses)
        {
            Console.WriteLine(
                $"{course.Name} | Цена: {course.Price:N2} | " +
                $"Свободных мест: {course.AvailablePlaces}");
        }
    }

    // Задание 5: транзакция с проверками существования студента
    // и отсутствия повторной записи. Обновление мест и INSERT — атомарно.
    public static async Task EnrollStudentAsync(int studentId, int courseId)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Студент существует?
            bool studentExists = await connection.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM students WHERE id = @StudentId);",
                new { StudentId = studentId },
                transaction);

            if (!studentExists)
                throw new InvalidOperationException("Студент не найден.");

            // 2. Повторной записи нет?
            bool alreadyEnrolled = await connection.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM student_courses
                    WHERE student_id = @StudentId AND course_id = @CourseId);
                """,
                new { StudentId = studentId, CourseId = courseId },
                transaction);

            if (alreadyEnrolled)
                throw new InvalidOperationException(
                    "Студент уже записан на этот курс.");

            // 3. Атомарно уменьшаем свободные места; RETURNING отдаёт новое значение.
            int? remainingPlaces = await connection.QuerySingleOrDefaultAsync<int?>(
                """
                UPDATE courses
                SET available_places = available_places - 1
                WHERE id = @CourseId AND available_places > 0
                RETURNING available_places;
                """,
                new { CourseId = courseId },
                transaction);

            if (remainingPlaces is null)
                throw new InvalidOperationException(
                    "Курс не найден или свободных мест нет.");

            // 4. INSERT записи на курс.
            await connection.ExecuteAsync(
                """
                INSERT INTO student_courses (student_id, course_id)
                VALUES (@StudentId, @CourseId);
                """,
                new { StudentId = studentId, CourseId = courseId },
                transaction);

            await transaction.CommitAsync();

            Console.WriteLine(
                $"Студент записан на курс. Осталось мест: {remainingPlaces}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Операция отменена: {ex.Message}");
        }
    }
}