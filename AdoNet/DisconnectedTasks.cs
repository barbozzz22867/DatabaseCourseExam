using System.Data;
using DatabaseCourseExam.Data;
using Npgsql;

namespace DatabaseCourseExam.AdoNet;

public static class DisconnectedTasks
{
    // Задание 13: помимо students — считаем количество записей на курсы
    // для каждого студента и выводим это число.
    public static async Task<DataTable> LoadStudentsAsync()
    {
        var table = new DataTable("Students");

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        const string sql = """
            SELECT id, first_name, last_name, age, group_id
            FROM students
            ORDER BY id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        using var adapter = new NpgsqlDataAdapter(command);

        adapter.Fill(table);
        table.PrimaryKey = [table.Columns["id"]!];

        // Отдельный запрос: количество курсов на каждого студента.
        var enrollments = new DataTable("Enrollments");
        await using var cmd2 = new NpgsqlCommand(
            """
            SELECT student_id, COUNT(*) AS cnt
            FROM student_courses
            GROUP BY student_id;
            """, connection);
        using var adapter2 = new NpgsqlDataAdapter(cmd2);
        adapter2.Fill(enrollments);

        var counts = enrollments.Rows.Cast<DataRow>()
            .ToDictionary(r => (int)r["student_id"], r => (long)r["cnt"]);

        Console.WriteLine("Количество записей на курсы по студентам:");
        foreach (DataRow row in table.Rows)
        {
            int id = (int)row["id"];
            long cnt = counts.TryGetValue(id, out var c) ? c : 0;
            Console.WriteLine(
                $"  {row["first_name"]} {row["last_name"]}: {cnt}");
        }

        return table;
    }

    public static void PrintTable(DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            Console.WriteLine(
                $"{row["id"]}: {row["first_name"]} " +
                $"{row["last_name"]}, возраст {row["age"]}");
        }
    }

    // Задание 7: поиск по первичному ключу, обработка отсутствия,
    // не менять строку, если значение не изменилось.
    // Задание 12: вывод RowState до и после изменения.
    public static void ChangeAgeInMemory(
        DataTable table,
        int studentId,
        int newAge)
    {
        DataRow? row = table.Rows.Find(studentId);

        if (row is null)
        {
            Console.WriteLine("Студент не найден в DataTable.");
            return;
        }

        Console.WriteLine($"RowState до изменения: {row.RowState}");

        if ((int)row["age"] == newAge)
        {
            Console.WriteLine(
                "Значение не изменилось — строка остаётся Unchanged.");
            return;
        }

        row["age"] = newAge;

        Console.WriteLine(
            $"RowState после изменения: {row.RowState} " +
            "(Unchanged → строка не менялась; " +
            "Modified → будет отправлена в UPDATE)");
    }

    // Задание 6: после Update вывести количество сохранённых строк
    // и проверить, остались ли строки в состоянии Modified.
    public static int SaveChanges(DataTable table)
    {
        using var connection = DatabaseConnection.Create();
        connection.Open();

        const string sql = """
            UPDATE students
            SET age = @age
            WHERE id = @id;
            """;

        using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(
            "@age", NpgsqlTypes.NpgsqlDbType.Integer, 4, "age");
        command.Parameters.Add(
            "@id", NpgsqlTypes.NpgsqlDbType.Integer, 4, "id");

        using var adapter = new NpgsqlDataAdapter();
        adapter.UpdateCommand = command;

        int affectedRows = adapter.Update(table);
        Console.WriteLine($"Сохранено изменений: {affectedRows}");

        int stillModified = table.Rows
            .Cast<DataRow>()
            .Count(r => r.RowState == DataRowState.Modified);

        Console.WriteLine(stillModified == 0
            ? "Все строки в состоянии Unchanged."
            : $"Внимание: осталось Modified строк — {stillModified}.");

        return affectedRows;
    }
}