using DatabaseCourseExam.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseCourseExam.EntityFramework;

public static class EntityFrameworkTasks
{
    // Задание 10: сгруппировать загруженных студентов по группе
    // и вывести количество студентов в каждой группе.
    public static async Task ShowStudentsAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        List<Student> students = await context.Students
            .Include(student => student.Group)
            .OrderBy(student => student.Id)
            .ToListAsync();

        foreach (Student student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.Group.Name}");
        }

        Console.WriteLine();
        Console.WriteLine("Количество студентов по группам:");

        var byGroup = students
            .GroupBy(student => student.Group.Name)
            .OrderBy(group => group.Key);

        foreach (var group in byGroup)
        {
            Console.WriteLine($"{group.Key}: {group.Count()}");
        }
    }

    // Задание 11: обработка DbUpdateException.
   
    public static async Task UpdateStudentAgeAsync(int studentId, int newAge)
    {
        await using var context = SchoolDbContextFactory.Create();

        Student? student = await context.Students
            .FirstOrDefaultAsync(x => x.Id == studentId);

        if (student is null)
        {
            Console.WriteLine("Студент не найден.");
            return;
        }

        student.Age = newAge;

        try
        {
            int affectedRows = await context.SaveChangesAsync();
            Console.WriteLine(
                $"Возраст изменён. Сохранено изменений: {affectedRows}");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(
                "Не удалось сохранить изменения (нарушено ограничение БД). " +
                $"Причина: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    // Задание 2: проекция только нужных полей + вычисляемый признак «мест нет».
    public static async Task ShowCoursesAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        var courses = await context.Courses
            .OrderBy(course => course.Name)
            .Select(course => new
            {
                course.Name,
                course.Price,
                course.AvailablePlaces,
                NoPlaces = course.AvailablePlaces <= 0
            })
            .ToListAsync();

        foreach (var course in courses)
        {
            Console.WriteLine(
                $"{course.Name} | Цена: {course.Price:N2} | " +
                $"Свободных мест: {course.AvailablePlaces}" +
                (course.NoPlaces ? " (мест нет)" : ""));
        }
    }
}