using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.Entities;

namespace Sadovod.Services;

public interface ISeedService
{
    Task SeedAsync();
}

public class SeedService : ISeedService
{
    private readonly SadovodDbContext _db;
    private readonly ILogger<SeedService> _log;

    public SeedService(SadovodDbContext db, ILogger<SeedService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task SeedAsync()
    {
        try
        {
            if (!await _db.Database.CanConnectAsync())
            {
                _log.LogWarning("Не удалось подключиться к БД. Пропускаю seed.");
                return;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ошибка подключения к БД при seed.");
            return;
        }

        await EnsureRolesAsync();
        await EnsureStatusesAsync();
        await EnsureCategoriesAsync();
        await EnsureShopSettingsAsync();
        await EnsureAdminAsync();
        await EnsureProductsAsync();
        await PromoteGrebnevAsync();
    }

    private async Task EnsureRolesAsync()
    {
        try
        {
            if (await _db.Roles.AnyAsync()) return;
            _db.Roles.AddRange(
                new Role { Name = "Администратор" },   // IDENTITY → Id=1
                new Role { Name = "Пользователь" });   // IDENTITY → Id=2
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { _log.LogError(ex, "Seed Roles"); }
    }

    private async Task EnsureStatusesAsync()
    {
        try
        {
            if (await _db.OrderStatuses.AnyAsync()) return;
            _db.OrderStatuses.AddRange(
                new OrderStatus { Name = "Новый" },      // IDENTITY → Id=1 (default в Orders.StatusId)
                new OrderStatus { Name = "Обработан" },  // → Id=2
                new OrderStatus { Name = "Выполнен" },   // → Id=3
                new OrderStatus { Name = "Отменён" });   // → Id=4
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { _log.LogError(ex, "Seed Statuses"); }
    }

    private async Task EnsureCategoriesAsync()
    {
        try
        {
            if (await _db.Categories.AnyAsync()) return;
            _db.Categories.AddRange(
                new Category { Name = "Семена овощей" },
                new Category { Name = "Семена цветов" },
                new Category { Name = "Удобрения" },
                new Category { Name = "Инструменты" });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { _log.LogError(ex, "Seed Categories"); }
    }

    private async Task EnsureShopSettingsAsync()
    {
        try
        {
            var s = await _db.ShopSettings.FirstOrDefaultAsync();
            const string about =
                "Интернет-магазин «Садовод» — это команда увлечённых садоводов, агрономов и предпринимателей, которая с 2008 года помогает дачникам и фермерам по всей России собирать богатый урожай.\n\n" +
                "В нашем каталоге — более 3 000 позиций: классические и редкие сорта овощей, ароматные пряные травы, эффектные однолетники и многолетники, профессиональные удобрения и надёжный садовый инвентарь.\n\n" +
                "Главная наша ценность — доверие покупателей. Мы даём гарантию всхожести семян, бережно упаковываем каждый заказ и доставляем его по всей России.\n\n" +
                "С «Садоводом» вы получаете не просто пакетик семян — вы получаете уверенность, что грядки будут радовать урожаем.";

            if (s is null)
            {
                _db.ShopSettings.Add(new ShopSetting
                {
                    Email = "info@sadovod.ru",
                    Phone = "+79990001122",
                    Address = "г. Москва, ул. Зелёная, д.1",
                    AboutUs = about
                });
                await _db.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(s.AboutUs) || s.AboutUs.Length < 200)
            {
                s.AboutUs = about;
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex) { _log.LogError(ex, "Seed ShopSettings"); }
    }

    private async Task EnsureAdminAsync()
    {
        try
        {
            if (await _db.Users.AnyAsync()) return;
            var password = GeneratePassword(14);
            _db.Users.Add(new User
            {
                Login = "admin@seedshop.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = "Администратор",
                Phone = "+79990000001",
                RoleId = 1
            });
            await _db.SaveChangesAsync();

            Console.WriteLine();
            Console.WriteLine("==============================================================");
            Console.WriteLine(" СОЗДАН АДМИНИСТРАТОР");
            Console.WriteLine("   Логин:  admin@seedshop.ru");
            Console.WriteLine($"   Пароль: {password}");
            Console.WriteLine("   Сохраните этот пароль — он отображается только сейчас.");
            Console.WriteLine("==============================================================");
            Console.WriteLine();
        }
        catch (Exception ex) { _log.LogError(ex, "Seed Admin"); }
    }

    private async Task EnsureProductsAsync()
    {
        try
        {
            if (await _db.Products.AnyAsync()) return;
            var cats = await _db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
            int? Cat(string n) => cats.TryGetValue(n, out var id) ? id : null;
            const string img = "/images/products/no-photo.png";

            _db.Products.AddRange(
                new Product { Name = "Томат «Бычье сердце»", Description = "Крупноплодный салатный сорт, плоды до 600 г, мясистая мякоть.", Price = 59m, Quantity = 120, CategoryId = Cat("Семена овощей"), Variety = "Среднеспелый", ImageUrl = img },
                new Product { Name = "Огурец «Зозуля F1»", Description = "Партенокарпический гибрид для теплиц и открытого грунта.", Price = 45m, Quantity = 200, CategoryId = Cat("Семена овощей"), Variety = "Раннеспелый", ImageUrl = img },
                new Product { Name = "Морковь «Нантская 4»", Description = "Сладкая морковь длиной 15-17 см, отлично хранится.", Price = 35m, Quantity = 350, CategoryId = Cat("Семена овощей"), Variety = "Среднеспелый", ImageUrl = img },
                new Product { Name = "Петуния «Каскад»", Description = "Ампельная петуния для балконов и подвесных кашпо, обильное цветение.", Price = 79m, Quantity = 80, CategoryId = Cat("Семена цветов"), Variety = "Махровая", ImageUrl = img },
                new Product { Name = "Бархатцы «Кармен»", Description = "Низкорослые бархатцы, цветут до заморозков, неприхотливы.", Price = 39m, Quantity = 220, CategoryId = Cat("Семена цветов"), Variety = "Низкорослые", ImageUrl = img },
                new Product { Name = "Удобрение «Фертика Люкс»", Description = "Универсальное минеральное удобрение для овощей и цветов, 1 кг.", Price = 320m, Quantity = 60, CategoryId = Cat("Удобрения"), Variety = "Минеральное", ImageUrl = img },
                new Product { Name = "Секатор «Садовод Pro»", Description = "Профессиональный секатор с тефлоновым покрытием лезвий.", Price = 890m, Quantity = 25, CategoryId = Cat("Инструменты"), Variety = "Профессиональный", ImageUrl = img },
                new Product { Name = "Лейка садовая 10 л", Description = "Пластиковая лейка с насадкой-распылителем, морозостойкая.", Price = 450m, Quantity = 40, CategoryId = Cat("Инструменты"), Variety = "Бытовая", ImageUrl = img });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { _log.LogError(ex, "Seed Products"); }
    }

    /// <summary>
    /// Идемпотентно назначает роль «Администратор» пользователю «Гребнев Даниил Александрович».
    /// Если пользователь не найден — ничего не делает.
    /// </summary>
    private async Task PromoteGrebnevAsync()
    {
        try
        {
            const string fullName = "Гребнев Даниил Александрович";
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.FullName == fullName);

            if (user is null)
            {
                _log.LogInformation("Пользователь «{Name}» не найден — пропускаю повышение.", fullName);
                return;
            }

            if (user.RoleId == 1)
            {
                _log.LogInformation("Пользователь «{Name}» уже имеет роль Администратор.", fullName);
                return;
            }

            user.RoleId = 1;
            await _db.SaveChangesAsync();
            _log.LogInformation("Пользователь «{Name}» (Id={Id}) повышен до Администратора.", fullName, user.Id);
        }
        catch (Exception ex) { _log.LogError(ex, "PromoteGrebnev"); }
    }

    private static string GeneratePassword(int length)
    {
        const string alpha = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var bytes = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (int i = 0; i < length; i++) chars[i] = alpha[bytes[i] % alpha.Length];
        return new string(chars);
    }
}
