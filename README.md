# 🌱 Интернет-магазин «Садовод»

Учебный проект — полноценный интернет-магазин семян, удобрений и садового инвентаря, реализованный на **ASP.NET Core MVC (.NET 10)** с использованием **SQL Server**. Включает публичную витрину, личный кабинет покупателя, корзину, оформление заказов и полноценную административную панель.

---

## 🆕 Что обновилось и доработалось

Доработки сверх первоначального ТЗ (подробное описание — в [`ТЗ/Тех_задание_2.md`](ТЗ/Тех_задание_2.md)). Все изменения выполнены **без добавления новых таблиц и столбцов в БД**.

### 🐛 Исправлен дефект: не работало оформление заказа
Команда вызова процедуры `dbo.SetSessionUserContext` не привязывалась к активной транзакции, из-за чего оформление заказа (`BeginTransactionAsync`) всегда падало с ошибкой «Не удалось создать заказ». В `SadovodDbContext.SetSessionUserContextAsync` добавлена привязка команды к текущей транзакции (`Database.CurrentTransaction?.GetDbTransaction()`). **Заказы оформляются корректно.**

### 🆕 Управление заказами в админке (новая вкладка «Заказы»)
Закрыт пробел из ТЗ (п. 17). Раздел `/Admin/Orders`: список всех заказов с покупателем, составом и суммой, фильтр по статусу, пагинация, **смена статуса** (Новый → Обработан → Выполнен → Отменён). При смене статуса покупателю отправляется **адресное push-уведомление** (новый метод `PushNotificationService.SendToUserAsync`).

### 🆕 Расширенные фильтры и сортировка каталога
На странице `/Products` добавлены фильтр по **диапазону цены** («от»/«до»), фильтр **«только в наличии»** и **сортировка** (по названию, по цене ↑↓, по остатку). Параметры сохраняются при пагинации. Реализовано динамическим построением `Where`/`OrderBy` в `ProductsController`.

### 🆕 Управление пользователями и ролями (новая вкладка «Пользователи»)
Раздел `/Admin/Users`: список учётных записей и **смена роли** (Администратор ↔ Пользователь) с защитой от понижения собственной учётной записи. Триггер аудита `trg_Users_Update` расширен (`CREATE OR ALTER`) — теперь фиксирует и смену роли (`RoleId`) в журнале аудита.

Полный комплект документации проекта разложен по папкам: [`ТЗ/`](ТЗ/) (технические задания), [`Руководства/`](Руководства/) (руководства пользователя), [`Отчёты/`](Отчёты/) (отчёты по практике).

> ⚠️ Чтобы заработал аудит смены роли, один раз повторно примените скрипт БД:
> `sqlcmd -S .\SQLEXPRESS -E -d SadovodShop -i "Scripts\InitDatabase.sql"`
> (скрипт идемпотентен, данные не затрагиваются). Остальные доработки работают сразу после сборки.

---

## 🛠 Стек технологий

| Слой | Технология |
|---|---|
| Фреймворк | ASP.NET Core MVC 10 |
| База данных | SQL Server (локальный экземпляр `.\SQLEXPRESS`) |
| ORM | Entity Framework Core 9 (Database First) |
| Аутентификация | Cookie Authentication + BCrypt.Net-Next |
| Фронтенд | Bootstrap 5, Razor Views, jQuery Validation |
| Карта | Leaflet.js + Nominatim (геокодирование) |
| Push-уведомления | Web Push API (VAPID), `WebPush` NuGet |
| Кэш | `IMemoryCache` (in-process) |
| Изображения | Сохранение в `wwwroot/images/products/`, `IFormFile`, `Guid`-имена |

---

## 📁 Структура проекта

```
Sadovod/
├── Program.cs                        # Точка входа: DI, middleware, seed при старте
├── Sadovod.csproj                    # net10.0, пакеты: BCrypt, EF Core, WebPush
├── appsettings.json                  # Строка подключения, VAPID-ключи, логирование
├── appsettings.Development.json      # Переопределения для режима разработки
│
├── Data/
│   └── SadovodDbContext.cs           # DbContext: все DbSet-ы, конфигурация EF, аудит
│
├── Models/
│   ├── Entities/                     # POCO-сущности (зеркало таблиц БД)
│   │   ├── Role.cs                   # Id, Name
│   │   ├── User.cs                   # Id, Login, PasswordHash, FullName, Phone, RoleId
│   │   ├── Category.cs               # Id, Name
│   │   ├── Product.cs                # Id, Name, Description, Price, Quantity, CategoryId, Variety, ImageUrl
│   │   ├── Order.cs                  # Id, UserId, OrderDate, StatusId, TotalAmount
│   │   ├── OrderItem.cs              # Id, OrderId, ProductId, Quantity, UnitPrice
│   │   ├── OrderStatus.cs            # Id, Name
│   │   ├── ShopSetting.cs            # Id, Email, Phone, Address, AboutUs
│   │   ├── AuditLog.cs               # Id, TableName, Operation, OldValue, NewValue, UserId, ChangeDate
│   │   └── PushSubscription.cs       # Id, UserId, Endpoint, P256dh, Auth
│   │
│   └── ViewModels/                   # Модели для передачи данных во View
│       ├── LoginViewModel.cs
│       ├── RegisterViewModel.cs
│       ├── ProfileViewModel.cs
│       ├── ProductListViewModel.cs   # Каталог: товары + пагинация + фильтры
│       ├── ProductEditViewModel.cs   # Форма создания/редактирования товара (+ IFormFile)
│       ├── CartItemViewModel.cs
│       ├── CheckoutViewModel.cs      # Форма оформления заказа
│       ├── DashboardViewModel.cs     # Дашборд: счётчики + данные для графика
│       ├── AuditLogFilterViewModel.cs
│       ├── SettingsViewModel.cs
│       └── PushSendViewModel.cs
│
├── Services/
│   ├── AuthService.cs                # Регистрация, вход (BCrypt), cookie SignIn/SignOut
│   ├── SeedService.cs                # Первичное заполнение БД при старте приложения
│   ├── CartService.cs                # Корзина в Session (JSON-сериализация Dictionary<int,int>)
│   ├── ImageService.cs               # Сохранение и удаление файлов изображений товаров
│   ├── SettingsService.cs            # Настройки магазина + категории с кэшированием
│   └── PushNotificationService.cs    # Отправка Web Push (VAPID) всем подписчикам
│
├── Controllers/                      # Публичная часть сайта
│   ├── HomeController.cs             # Главная (топ-товары), О нас, Контакты, Ошибки
│   ├── ProductsController.cs         # Каталог с поиском/фильтрацией/пагинацией, Детали товара
│   ├── AccountController.cs          # Вход, Регистрация, Профиль, Выход
│   ├── CartController.cs             # Корзина: просмотр, добавление, обновление, удаление
│   ├── OrdersController.cs           # Оформление заказа (транзакция), история заказов
│   └── PushController.cs             # API: сохранение подписки на push-уведомления
│
├── Areas/Admin/                      # Административная панель (политика AdminOnly)
│   └── Controllers/
│       ├── DashboardController.cs    # Дашборд: статистика + график продаж за 14 дней
│       ├── AdminProductsController.cs# CRUD товаров + загрузка/удаление изображений
│       ├── AuditLogController.cs     # Просмотр журнала аудита с фильтрацией и пагинацией
│       ├── SettingsController.cs     # Редактирование реквизитов магазина
│       └── PushController.cs         # Рассылка push-уведомлений всем подписчикам
│
├── Views/                            # Razor-шаблоны публичной части
│   ├── Shared/
│   │   ├── _Layout.cshtml            # Главный макет: навигация, переключатель темы, футер
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Home/                         # Index, About, Contacts, Error
│   ├── Products/                     # Index (каталог), Details (карточка товара)
│   ├── Account/                      # Login, Register, Profile
│   ├── Cart/                         # Index (корзина)
│   └── Orders/                       # Create (оформление), MyOrders (история)
│
├── Areas/Admin/Views/                # Razor-шаблоны админки
│   ├── Shared/_AdminLayout.cshtml    # Отдельный макет: боковое меню, хлебные крошки
│   ├── Dashboard/Index.cshtml        # Карточки со статистикой + Chart.js
│   ├── AdminProducts/
│   │   ├── Index.cshtml              # Таблица товаров с миниатюрами 48×48
│   │   ├── Create.cshtml             # Форма создания (multipart/form-data + file input)
│   │   ├── Edit.cshtml               # Форма редактирования + превью текущего фото
│   │   └── Delete.cshtml             # Страница подтверждения удаления
│   ├── AuditLog/Index.cshtml         # Журнал с фильтрами по таблице/операции/дате
│   ├── Settings/Index.cshtml         # Форма реквизитов магазина
│   └── Push/Index.cshtml             # Форма отправки push-уведомления
│
└── wwwroot/
    ├── css/site.css                  # CSS-переменные :root и [data-theme="dark"], темы
    ├── js/site.js                    # Переключатель темы (localStorage + анимация spin)
    ├── js/push.js                    # Подписка браузера на Web Push (ServiceWorker)
    ├── service-worker.js             # Приём push-событий, клик по уведомлению
    └── images/products/
        ├── no-photo.png              # Заглушка для товаров без фото (никогда не удаляется)
        └── <guid>.<ext>              # Загруженные изображения товаров
```

---

## 🗄 База данных

**СУБД:** SQL Server, экземпляр `.\SQLEXPRESS`, база `SadovodShop`.

### Таблицы

| Таблица | Назначение |
|---|---|
| `Roles` | Справочник ролей: 1 — Администратор, 2 — Пользователь |
| `Users` | Учётные записи. `Login` уникален, `RoleId` по умолчанию = 2 |
| `Categories` | Категории товаров (уникальные имена) |
| `Products` | Товарный каталог. `Price` — `decimal(10,2)`, `ImageUrl` — путь к файлу |
| `OrderStatuses` | 1 — Новый, 2 — Обработан, 3 — Выполнен, 4 — Отменён |
| `Orders` | Заказы. `StatusId` по умолчанию = 1 (Новый), `OrderDate` = `GETDATE()` |
| `OrderItems` | Позиции заказа. Cascade delete при удалении заказа |
| `ShopSettings` | Одна запись: email, телефон, адрес, текст «О нас» |
| `AuditLog` | Автоматическая история изменений (через триггеры) |
| `PushSubscriptions` | VAPID-подписки браузеров на push-уведомления |

### Триггеры и хранимые процедуры

- **`trg_Products_Insert` / `_Update` / `_Delete`** — при каждом изменении товара пишут запись в `AuditLog` с JSON старого и нового значений.
- **`trg_Users_Update`** — аналогично для изменений пользователей.
- **`dbo.SetSessionUserContext(@UserId)`** — устанавливает `SESSION_CONTEXT(N'UserId')`, чтобы триггеры знали, кто сделал изменение.

`SadovodDbContext.SaveChangesAsync()` перед каждым коммитом автоматически вызывает эту хранимую процедуру, передавая Id текущего авторизованного пользователя из `ClaimTypes.NameIdentifier`.

---

## ⚡ Быстрый старт

### Требования

- .NET SDK 10.0+
- SQL Server Express (или любой экземпляр, укажите строку подключения)
- Браузер с поддержкой Web Push (для push-уведомлений)

### 1. Создать базу данных

```powershell
# Создать базу данных (если ещё не создана)
sqlcmd -S .\SQLEXPRESS -E -Q "IF DB_ID('SadovodShop') IS NULL CREATE DATABASE SadovodShop;"

# Применить полную схему БД из скрипта
sqlcmd -S .\SQLEXPRESS -E -d SadovodShop -i "Scripts\InitDatabase.sql"
```

Скрипт `Scripts\InitDatabase.sql` содержит полную схему базы данных: все таблицы, внешние ключи, индексы, триггеры аудита, хранимые процедуры и начальные данные (роли, статусы, категории, товары-примеры). Скрипт **идемпотентен** — безопасно запускать повторно, существующие данные не затрагиваются.

При следующем запуске приложения `SeedService` проверит наличие данных и при необходимости дозаполнит пропущенное.

### 2. Настроить строку подключения

В `appsettings.json` задана строка по умолчанию:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=SadovodShop;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

Если SQL Server расположен на другом хосте — поменяйте `Server`.

### 3. Настроить VAPID-ключи (для push-уведомлений)

Сгенерируйте пару ключей любым VAPID-генератором (например, `web-push generate-vapid-keys`) и вставьте в `appsettings.json`:

```json
"PushSettings": {
  "Subject": "mailto:your@email.com",
  "PublicKey": "ваш_публичный_ключ",
  "PrivateKey": "ваш_приватный_ключ"
}
```

> Без VAPID-ключей push-рассылка будет недоступна, остальное работает.

### 4. Запустить

```powershell
cd "E:\Прокет ПП\Садовод"
dotnet restore
dotnet run
```

Приложение запустится на `http://localhost:5000`.

### 5. Первый запуск — что происходит автоматически

При старте `SeedService` выполняет следующие идемпотентные шаги:

| Шаг | Условие выполнения | Результат |
|---|---|---|
| `EnsureRolesAsync` | Таблица `Roles` пуста | Создаёт роли «Администратор» (Id=1) и «Пользователь» (Id=2) |
| `EnsureStatusesAsync` | Таблица `OrderStatuses` пуста | Создаёт 4 статуса заказа |
| `EnsureCategoriesAsync` | Таблица `Categories` пуста | Создаёт 4 категории товаров |
| `EnsureShopSettingsAsync` | Нет записи или `AboutUs` короткий | Создаёт/дополняет настройки магазина |
| `EnsureAdminAsync` | Таблица `Users` пуста | Создаёт admin-аккаунт `admin@seedshop.ru` с рандомным паролем (пароль выводится **один раз** в консоль) |
| `EnsureProductsAsync` | Таблица `Products` пуста | Создаёт 8 товаров-примеров с заглушкой `no-photo.png` |
| `PromoteGrebnevAsync` | Всегда | Если в БД есть пользователь с `FullName = 'Гребнев Даниил Александрович'` и `RoleId ≠ 1` — повышает его до администратора |

---

## 🔐 Аутентификация и роли

- **Cookie-based аутентификация.** Куки: `.Sadovod.Auth`, срок действия — 14 дней скользящий (30 дней при «Запомнить меня»).
- **Хеширование паролей** — BCrypt с work factor по умолчанию.
- **Роли:** `Администратор` (RoleId=1) и `Пользователь` (RoleId=2).
- **Политика `AdminOnly`** — все контроллеры в `Areas/Admin` требуют роль «Администратор». При отсутствии доступа — редирект на `/Account/Login`.
- **Claims в cookie:** `NameIdentifier` (Id), `Name` (Login), `FullName`, `Role`.

---

## 🛒 Публичная часть

### Главная страница (`/`)
Выводит **4 товара с наибольшим остатком** (т.е. популярных в наличии). Каждая карточка содержит фото, название, цену и кнопку «В корзину».

### Каталог товаров (`/Products`)
- Поиск по названию (SQL `LIKE`).
- Фильтрация по категории.
- Пагинация — **12 товаров на страницу**.
- Категории берутся из кэша (`IMemoryCache`, 30 мин).

### Карточка товара (`/Products/Details/{id}`)
Полное описание, сорт, остаток, цена, изображение, кнопка «Добавить в корзину».

### Корзина (`/Cart`)
- Хранится в **серверной сессии** (ключ `"Cart"`, формат JSON — `Dictionary<int, int>` productId → quantity).
- При добавлении проверяется наличие товара на складе — нельзя добавить больше, чем есть.
- Обновление количества и удаление позиций.
- Итоговая сумма пересчитывается автоматически.

### Оформление заказа (`/Orders/Create`)
- Доступно только авторизованным пользователям.
- Форма заполняется данными из профиля (ФИО, телефон).
- **Транзакция:** перед созданием заказа проверяется наличие всех товаров, затем создаётся `Order`, его позиции (`OrderItems`) и уменьшаются остатки на складе. При ошибке — откат.
- После успеха корзина очищается.

### История заказов (`/Orders/MyOrders`)
Список заказов текущего пользователя с составом и статусом.

### О нас (`/Home/About`) и Контакты (`/Home/Contacts`)
Данные берутся из таблицы `ShopSettings` через кэшированный `ISettingsService`. На странице контактов — интерактивная карта **Leaflet** с геокодированием адреса через **Nominatim**. Если геокодинг недоступен — маркер ставится на Москву (55.7558, 37.6173).

---

## 👤 Личный кабинет

- **Регистрация** (`/Account/Register`) — создаёт пользователя с `RoleId=2` (Пользователь).
- **Вход** (`/Account/Login`) — валидация логина/пароля через BCrypt, опция «Запомнить меня».
- **Профиль** (`/Account/Profile`) — изменение ФИО и телефона (логин менять нельзя).
- **Выход** (`/Account/Logout`) — уничтожает cookie.

---

## 🖥 Административная панель (`/Admin`)

Доступна только пользователям с ролью **Администратор**.

### Дашборд (`/Admin/Dashboard`)
Четыре карточки-счётчика:
- **Товаров** — общее количество записей в `Products`.
- **Пользователей** — общее количество в `Users`.
- **Заказов** — общее количество в `Orders`.
- **Выручка (выполненные)** — сумма `TotalAmount` заказов со статусом «Выполнен» (Id=3).

**График продаж** — линейный Chart.js за последние 14 дней (только выполненные заказы).

### Управление товарами (`/Admin/AdminProducts`)

| Действие | Описание |
|---|---|
| Список | Таблица с миниатюрами 48×48, названием, категорией, сортом, ценой, остатком |
| Создание | Форма с полями: название, категория, цена, количество, сорт, описание, **файл изображения** |
| Редактирование | То же + превью текущего фото. При загрузке нового — старое автоматически удаляется |
| Удаление | Страница подтверждения. Удаляет товар и его изображение с диска |

#### 📷 Работа с изображениями

- Допустимые форматы: **jpg, jpeg, png, webp**.
- Максимальный размер файла: **5 МБ**.
- Файл сохраняется в `wwwroot/images/products/` под именем `{Guid.NewGuid():N}.{расширение}`.
- В базу данных записывается относительный путь: `/images/products/<имя файла>`.
- Если при создании товара файл не выбран — устанавливается заглушка `/images/products/no-photo.png`.

**Удаление старого файла при замене:**
Когда администратор редактирует товар и загружает новое изображение, старый файл **автоматически удаляется с диска** до сохранения нового. Исключение — файл `no-photo.png`: заглушка никогда не удаляется, так как используется для товаров без фото.

Логика реализована в `ImageService.DeleteIfLocal(url)`:
```csharp
// Пропускает placeholder — он нужен как заглушка
if (string.Equals(url, "/images/products/no-photo.png", OrdinalIgnoreCase)) return;
// Пропускает внешние URL (не начинающиеся с "/")
if (!url.StartsWith("/")) return;
// Удаляет локальный файл, если он существует
File.Delete(Path.Combine(webRootPath, url.TrimStart('/')));
```

Этот же метод вызывается при **удалении товара** — файл изображения тоже удаляется с диска автоматически.

### Журнал аудита (`/Admin/AuditLog`)
Все изменения в таблицах `Products` и `Users` фиксируются SQL-триггерами. Просмотр с фильтрами:
- По таблице (`Products` / `Users`).
- По операции (Создание / Обновление / Удаление).
- По диапазону дат.
- Пагинация.

Каждая запись содержит: дату, таблицу, операцию, логин пользователя, **JSON старых и новых значений**.

### Настройки магазина (`/Admin/Settings`)
Редактирование: email, телефон, адрес и текст «О нас». После сохранения кэш `ISettingsService` инвалидируется — изменения видны на сайте мгновенно.

### Push-уведомления (`/Admin/Push`)
Отправка Web Push всем подписчикам: заголовок, текст, ссылка. Подписки с ответом `410 Gone` или `404` автоматически удаляются.

---

## 🎨 Темы оформления

Переключатель тёмной/светлой темы реализован на CSS-переменных:

```css
:root { --bg: #ffffff; --text: #212529; ... }
[data-theme="dark"] { --bg: #1a1a1a; --text: #e0e0e0; ... }
```

Кнопка `#theme-toggle` переключает атрибут `data-theme` на `<html>` и сохраняет выбор в `localStorage`. При загрузке страницы тема применяется до рендера (без мигания). Смена сопровождается анимацией `spin` иконки 🌙/☀️.

---

## 🔔 Push-уведомления

1. Браузер запрашивает разрешение (`Notification.requestPermission()`).
2. ServiceWorker регистрирует VAPID-подписку через `PushManager.subscribe(publicKey)`.
3. Подписка отправляется на `/Push/Subscribe` (JSON: endpoint, p256dh, auth).
4. Сервер сохраняет в таблице `PushSubscriptions`.
5. Администратор отправляет рассылку из `/Admin/Push`.
6. `PushNotificationService` обходит все подписки и отправляет уведомление через `WebPushClient`.
7. ServiceWorker получает событие `push`, показывает уведомление.
8. По клику (`notificationclick`) открывается указанная URL.

---

## 🏗 Архитектурные решения

### Культура и локаль
Приложение принудительно использует `CultureInfo.InvariantCulture` через `UseRequestLocalization`. Это гарантирует десятичный разделитель **точка** (`39.00`) как при рендере форм, так и при модел-байндинге — без конфликтов с jQuery Validation на русской ОС.

### Кэширование
`SettingsService` кэширует данные через `IMemoryCache`:
- `ShopSettings` — 15 минут.
- `Categories` — 30 минут.
При сохранении настроек вызывается `InvalidateAll()` — оба ключа удаляются из кэша.

### Транзакции
Создание заказа обёрнуто в `BeginTransactionAsync()`. Если на каком-либо товаре не хватает остатков — транзакция откатывается, пользователь видит ошибку. Это исключает «гонку» при одновременных заказах.

### Аудит
`SadovodDbContext` переопределяет `SaveChangesAsync`, вызывая перед сохранением ХП `SetSessionUserContext`. Триггеры на уровне SQL Server читают `SESSION_CONTEXT(N'UserId')` и записывают строку в `AuditLog`. Это позволяет знать, кто именно сделал изменение, даже если несколько соединений работают параллельно.

---

## 📦 NuGet-пакеты

| Пакет | Версия | Назначение |
|---|---|---|
| `BCrypt.Net-Next` | 4.0.3 | Хеширование и проверка паролей |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.0 | ORM + драйвер SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.0 | `dotnet ef` (dev-зависимость) |
| `WebPush` | 1.0.12 | Отправка Web Push уведомлений (VAPID) |

---

## 🔧 Полезные команды

```powershell
# Сборка
dotnet build

# Запуск
dotnet run

# Проверить пользователей в БД
sqlcmd -S ".\SQLEXPRESS" -d "SadovodShop" -Q "SELECT Id, Login, FullName, RoleId FROM Users"

# Проверить заказы
sqlcmd -S ".\SQLEXPRESS" -d "SadovodShop" -Q "SELECT * FROM Orders ORDER BY OrderDate DESC"

# Последние 20 записей аудита
sqlcmd -S ".\SQLEXPRESS" -d "SadovodShop" -Q "SELECT TOP 20 * FROM AuditLog ORDER BY ChangeDate DESC"
```

---

## ⚠️ Известные особенности

- `NU1903` (Newtonsoft.Json 10.0.3) — предупреждение NuGet о уязвимости транзитивной зависимости `WebPush`. Не влияет на работу.
- Push-уведомления работают только по **HTTPS** (или `localhost`). На HTTP браузер отказывает в доступе к `PushManager`.
- Карта Nominatim имеет [Rate Limit](https://operations.osmfoundation.org/policies/nominatim/) (1 запрос/сек). Для production — замените на платный геокодер.
