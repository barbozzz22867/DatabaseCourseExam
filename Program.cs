// Задание 8: везде int.TryParse, неверный ввод не завершает программу.
// Задание 3:  пункт 15 — сравнение ADO.NET / EF Core / Dapper.


using System.Data;
using DatabaseCourseExam.AdoNet;
using DatabaseCourseExam.DapperTasks;
using DatabaseCourseExam.EntityFramework;

DataTable? studentsTable = null;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("=== Зачёт по работе с БД ===");
    Console.WriteLine("1. ADO.NET: показать студентов (с фильтром по group_id)");
    Console.WriteLine("2. ADO.NET: изменить возраст студента");
    Console.WriteLine("3. ADO.NET: записать студента на курс");
    Console.WriteLine("4. Загрузить студентов в DataTable");
    Console.WriteLine("5. Изменить возраст в DataTable");
    Console.WriteLine("6. Показать DataTable");
    Console.WriteLine("7. Сохранить изменения DataTable");
    Console.WriteLine("8. EF Core: показать студентов + группировка");
    Console.WriteLine("9. EF Core: изменить возраст студента");
    Console.WriteLine("10. EF Core: показать курсы");
    Console.WriteLine("11. Dapper: показать студентов (с фильтром по группе)");
    Console.WriteLine("12. Dapper: изменить возраст");
    Console.WriteLine("13. Dapper: показать курсы");
    Console.WriteLine("14. Dapper: записать студента на курс");
    Console.WriteLine("15. Сравнить вывод: ADO.NET / EF Core / Dapper");
    Console.WriteLine("0. Выход");
    Console.Write("Выберите действие: ");

    string? choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                {
                    Console.Write("group_id (Enter — без фильтра): ");
                    string? raw = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        await AdoNetTasks.ShowStudentsAsync();
                    }
                    else if (int.TryParse(raw, out int gid))
                    {
                        await AdoNetTasks.ShowStudentsAsync(gid);
                    }
                    else
                    {
                        Console.WriteLine("Некорректный group_id.");
                    }
                    break;
                }

            case "2":
                {
                    Console.Write("ID студента: ");
                    if (!int.TryParse(Console.ReadLine(), out int studentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");
                    if (!int.TryParse(Console.ReadLine(), out int age)
                        || age is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    await AdoNetTasks.UpdateStudentAgeAsync(studentId, age);
                    break;
                }

            case "3":
                {
                    Console.Write("ID студента: ");
                    if (!int.TryParse(Console.ReadLine(), out int enrollSid))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("ID курса: ");
                    if (!int.TryParse(Console.ReadLine(), out int enrollCid))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    await AdoNetTasks.EnrollStudentAsync(enrollSid, enrollCid);
                    break;
                }

            case "4":
                studentsTable = await DisconnectedTasks.LoadStudentsAsync();
                Console.WriteLine("Данные загружены в память.");
                DisconnectedTasks.PrintTable(studentsTable);
                break;

            case "5":
                {
                    if (studentsTable is null)
                    {
                        Console.WriteLine("Сначала загрузите данные (пункт 4).");
                        break;
                    }

                    Console.Write("ID студента: ");
                    if (!int.TryParse(Console.ReadLine(), out int dSid))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");
                    if (!int.TryParse(Console.ReadLine(), out int dAge)
                        || dAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    DisconnectedTasks.ChangeAgeInMemory(
                        studentsTable, dSid, dAge);
                    break;
                }

            case "6":
                if (studentsTable is null)
                {
                    Console.WriteLine("Сначала загрузите данные (пункт 4).");
                    break;
                }
                DisconnectedTasks.PrintTable(studentsTable);
                break;

            case "7":
                if (studentsTable is null)
                {
                    Console.WriteLine("Сначала загрузите данные (пункт 4).");
                    break;
                }
                DisconnectedTasks.SaveChanges(studentsTable);
                break;

            case "8":
                await EntityFrameworkTasks.ShowStudentsAsync();
                break;

            case "9":
                {
                    Console.Write("ID студента: ");
                    if (!int.TryParse(Console.ReadLine(), out int efSid))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");
                    if (!int.TryParse(Console.ReadLine(), out int efAge)
                        || efAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    await EntityFrameworkTasks.UpdateStudentAgeAsync(efSid, efAge);
                    break;
                }

            case "10":
                await EntityFrameworkTasks.ShowCoursesAsync();
                break;

            case "11":
                {
                    Console.Write("Название группы (Enter — без фильтра): ");
                    string? groupName = Console.ReadLine();

                    await DapperStudentTasks.ShowStudentsAsync(
                        string.IsNullOrWhiteSpace(groupName) ? null : groupName);
                    break;
                }

            case "12":
                {
                    Console.Write("ID студента: ");
                    if (!int.TryParse(Console.ReadLine(), out int dapSid))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");
                    if (!int.TryParse(Console.ReadLine(), out int dapAge)
                        || dapAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    await DapperStudentTasks.UpdateStudentAgeAsync(dapSid, dapAge);
                    break;
                }

            case "13":
                await DapperStudentTasks.ShowCoursesAsync();
                break;

            case "14":
                {
                    Console.Write("ID студента: ");
                    if (!int.TryParse(Console.ReadLine(), out int dEnrollSid))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("ID курса: ");
                    if (!int.TryParse(Console.ReadLine(), out int dEnrollCid))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    await DapperStudentTasks.EnrollStudentAsync(
                        dEnrollSid, dEnrollCid);
                    break;
                }

            case "15":
                Console.WriteLine("--- ADO.NET ---");
                await AdoNetTasks.ShowStudentsAsync();
                Console.WriteLine("--- EF Core ---");
                await EntityFrameworkTasks.ShowStudentsAsync();
                Console.WriteLine("--- Dapper ---");
                await DapperStudentTasks.ShowStudentsAsync();
                break;

            case "0":
                return;

            default:
                Console.WriteLine("Неизвестная команда.");
                break;
        }
    }
    catch (FormatException)
    {
        Console.WriteLine("Введите целое число.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}