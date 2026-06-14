-- =============================================================================
--  Интернет-магазин «Садовод» — полный идемпотентный скрипт базы данных
--  База:   SadovodShop
--  СУБД:   SQL Server (.\SQLEXPRESS или любой другой экземпляр)
--  Версия: 3.0
--
--  Скрипт безопасно запускать повторно: все объекты создаются через
--  IF NOT EXISTS / IF OBJECT_ID IS NULL, поэтому существующие данные
--  и объекты не затрагиваются.
--
--  Порядок запуска:
--    sqlcmd -S .\SQLEXPRESS -E -Q "IF DB_ID('SadovodShop') IS NULL CREATE DATABASE SadovodShop;"
--    sqlcmd -S .\SQLEXPRESS -E -d SadovodShop -i "Scripts\InitDatabase.sql"
-- =============================================================================

USE SadovodShop;
GO

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- 1. ТАБЛИЦЫ
-- =============================================================================

-- -----------------------------------------------
-- 1.1 Roles — справочник ролей
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE dbo.Roles (
        Id   INT           NOT NULL IDENTITY(1,1),
        Name NVARCHAR(50)  NOT NULL,
        CONSTRAINT PK_Roles        PRIMARY KEY (Id),
        CONSTRAINT UQ_Roles_Name   UNIQUE      (Name)
    );
    PRINT 'Таблица Roles создана.';
END
GO

-- -----------------------------------------------
-- 1.2 Users — учётные записи
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE dbo.Users (
        Id           INT           NOT NULL IDENTITY(1,1),
        Login        NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName     NVARCHAR(150)     NULL,
        Phone        NVARCHAR(20)      NULL,
        RoleId       INT           NOT NULL CONSTRAINT DF_Users_RoleId DEFAULT (2),
        CONSTRAINT PK_Users      PRIMARY KEY (Id),
        CONSTRAINT UQ_Users_Login UNIQUE      (Login)
    );
    PRINT 'Таблица Users создана.';
END
GO

-- -----------------------------------------------
-- 1.3 Categories — категории товаров
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE dbo.Categories (
        Id   INT           NOT NULL IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_Categories      PRIMARY KEY (Id),
        CONSTRAINT UQ_Categories_Name UNIQUE      (Name)
    );
    PRINT 'Таблица Categories создана.';
END
GO

-- -----------------------------------------------
-- 1.4 Products — товарный каталог
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE dbo.Products (
        Id          INT             NOT NULL IDENTITY(1,1),
        Name        NVARCHAR(200)   NOT NULL,
        Description NVARCHAR(MAX)       NULL,
        Price       DECIMAL(10, 2)  NOT NULL,
        Quantity    INT             NOT NULL CONSTRAINT DF_Products_Quantity DEFAULT (0),
        CategoryId  INT                 NULL,
        Variety     NVARCHAR(100)       NULL,
        ImageUrl    NVARCHAR(500)       NULL,
        CONSTRAINT PK_Products PRIMARY KEY (Id)
    );
    PRINT 'Таблица Products создана.';
END
GO

-- -----------------------------------------------
-- 1.5 OrderStatuses — статусы заказов
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrderStatuses')
BEGIN
    CREATE TABLE dbo.OrderStatuses (
        Id   INT          NOT NULL IDENTITY(1,1),
        Name NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_OrderStatuses      PRIMARY KEY (Id),
        CONSTRAINT UQ_OrderStatuses_Name UNIQUE      (Name)
    );
    PRINT 'Таблица OrderStatuses создана.';
END
GO

-- -----------------------------------------------
-- 1.6 Orders — заказы
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE dbo.Orders (
        Id          INT            NOT NULL IDENTITY(1,1),
        UserId      INT                NULL,
        OrderDate   DATETIME       NOT NULL CONSTRAINT DF_Orders_OrderDate   DEFAULT (GETDATE()),
        StatusId    INT            NOT NULL CONSTRAINT DF_Orders_StatusId    DEFAULT (1),
        TotalAmount DECIMAL(10, 2) NOT NULL CONSTRAINT DF_Orders_TotalAmount DEFAULT (0),
        CONSTRAINT PK_Orders PRIMARY KEY (Id)
    );
    PRINT 'Таблица Orders создана.';
END
GO

-- -----------------------------------------------
-- 1.7 OrderItems — позиции заказов
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrderItems')
BEGIN
    CREATE TABLE dbo.OrderItems (
        Id        INT            NOT NULL IDENTITY(1,1),
        OrderId   INT            NOT NULL,
        ProductId INT            NOT NULL,
        Quantity  INT            NOT NULL,
        UnitPrice DECIMAL(10, 2) NOT NULL,
        CONSTRAINT PK_OrderItems PRIMARY KEY (Id)
    );
    PRINT 'Таблица OrderItems создана.';
END
GO

-- -----------------------------------------------
-- 1.8 ShopSettings — настройки магазина
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShopSettings')
BEGIN
    CREATE TABLE dbo.ShopSettings (
        Id      INT           NOT NULL IDENTITY(1,1),
        Email   NVARCHAR(100)     NULL,
        Phone   NVARCHAR(20)      NULL,
        Address NVARCHAR(300)     NULL,
        AboutUs NVARCHAR(MAX)     NULL,
        CONSTRAINT PK_ShopSettings PRIMARY KEY (Id)
    );
    PRINT 'Таблица ShopSettings создана.';
END
GO

-- -----------------------------------------------
-- 1.9 AuditLog — журнал изменений
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE dbo.AuditLog (
        Id        BIGINT        NOT NULL IDENTITY(1,1),
        TableName NVARCHAR(50)  NOT NULL,
        Operation CHAR(1)       NOT NULL,   -- I = Insert, U = Update, D = Delete
        OldValue  NVARCHAR(MAX)     NULL,
        NewValue  NVARCHAR(MAX)     NULL,
        UserId    INT               NULL,
        ChangeDate DATETIME     NOT NULL CONSTRAINT DF_AuditLog_ChangeDate DEFAULT (GETDATE()),
        CONSTRAINT PK_AuditLog PRIMARY KEY (Id)
    );
    PRINT 'Таблица AuditLog создана.';
END
GO

-- -----------------------------------------------
-- 1.10 PushSubscriptions — VAPID-подписки
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PushSubscriptions')
BEGIN
    CREATE TABLE dbo.PushSubscriptions (
        Id        INT           NOT NULL IDENTITY(1,1),
        UserId    INT               NULL,
        Endpoint  NVARCHAR(500) NOT NULL,
        P256dh    NVARCHAR(255) NOT NULL,
        Auth      NVARCHAR(255) NOT NULL,
        CreatedAt DATETIME      NOT NULL CONSTRAINT DF_PushSubs_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT PK_PushSubscriptions PRIMARY KEY (Id)
    );
    PRINT 'Таблица PushSubscriptions создана.';
END
GO

-- =============================================================================
-- 2. ВНЕШНИЕ КЛЮЧИ
-- =============================================================================

-- Users → Roles
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Roles')
BEGIN
    ALTER TABLE dbo.Users
        ADD CONSTRAINT FK_Users_Roles
        FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
    PRINT 'FK_Users_Roles создан.';
END
GO

-- Products → Categories
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Products_Categories')
BEGIN
    ALTER TABLE dbo.Products
        ADD CONSTRAINT FK_Products_Categories
        FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id)
        ON DELETE SET NULL
        ON UPDATE CASCADE;
    PRINT 'FK_Products_Categories создан.';
END
GO

-- Orders → Users
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_Users')
BEGIN
    ALTER TABLE dbo.Orders
        ADD CONSTRAINT FK_Orders_Users
        FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION;
    PRINT 'FK_Orders_Users создан.';
END
GO

-- Orders → OrderStatuses
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_Statuses')
BEGIN
    ALTER TABLE dbo.Orders
        ADD CONSTRAINT FK_Orders_Statuses
        FOREIGN KEY (StatusId) REFERENCES dbo.OrderStatuses (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION;
    PRINT 'FK_Orders_Statuses создан.';
END
GO

-- OrderItems → Orders (Cascade delete)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Orders')
BEGIN
    ALTER TABLE dbo.OrderItems
        ADD CONSTRAINT FK_OrderItems_Orders
        FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id)
        ON DELETE CASCADE
        ON UPDATE NO ACTION;
    PRINT 'FK_OrderItems_Orders создан.';
END
GO

-- OrderItems → Products
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Products')
BEGIN
    ALTER TABLE dbo.OrderItems
        ADD CONSTRAINT FK_OrderItems_Products
        FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION;
    PRINT 'FK_OrderItems_Products создан.';
END
GO

-- AuditLog → Users (Set Null при удалении пользователя)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AuditLog_Users')
BEGIN
    ALTER TABLE dbo.AuditLog
        ADD CONSTRAINT FK_AuditLog_Users
        FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
        ON DELETE SET NULL
        ON UPDATE CASCADE;
    PRINT 'FK_AuditLog_Users создан.';
END
GO

-- PushSubscriptions → Users (Set Null при удалении пользователя)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PushSubscriptions_Users')
BEGIN
    ALTER TABLE dbo.PushSubscriptions
        ADD CONSTRAINT FK_PushSubscriptions_Users
        FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
        ON DELETE SET NULL
        ON UPDATE NO ACTION;
    PRINT 'FK_PushSubscriptions_Users создан.';
END
GO

-- =============================================================================
-- 3. ХРАНИМЫЕ ПРОЦЕДУРЫ
-- =============================================================================

-- -----------------------------------------------
-- 3.1 SetSessionUserContext
--     Устанавливает SESSION_CONTEXT(N'UserId') перед каждым SaveChanges.
--     Триггеры аудита читают это значение, чтобы знать, кто сделал изменение.
-- -----------------------------------------------
IF OBJECT_ID('dbo.SetSessionUserContext', 'P') IS NULL
BEGIN
    EXEC('
    CREATE PROCEDURE dbo.SetSessionUserContext
        @UserId INT
    AS
    BEGIN
        IF @UserId IS NOT NULL
            EXEC sp_set_session_context N''UserId'', @UserId;
        ELSE
            EXEC sp_set_session_context N''UserId'', NULL;
    END
    ');
    PRINT 'Процедура SetSessionUserContext создана.';
END
GO

-- -----------------------------------------------
-- 3.2 RegisterUser
--     Регистрирует пользователя с хешем пароля через SHA2-512 + GUID-соль.
--     Используется только для быстрого создания пользователей напрямую из SQL.
--     Приложение регистрирует через BCrypt (AuthService.cs).
-- -----------------------------------------------
IF OBJECT_ID('dbo.RegisterUser', 'P') IS NULL
BEGIN
    EXEC('
    CREATE PROCEDURE dbo.RegisterUser
        @Login    NVARCHAR(100),
        @Password NVARCHAR(100),
        @FullName NVARCHAR(150),
        @Phone    NVARCHAR(20),
        @RoleId   INT = 2,
        @NewUserId INT OUTPUT
    AS
    BEGIN
        SET NOCOUNT ON;
        DECLARE @Salt UNIQUEIDENTIFIER = NEWID();
        DECLARE @Hash NVARCHAR(255) = CONVERT(NVARCHAR(255),
            HASHBYTES(''SHA2_512'', @Password + CAST(@Salt AS NVARCHAR(36))), 2);
        INSERT INTO dbo.Users (Login, PasswordHash, FullName, Phone, RoleId)
        VALUES (@Login, @Hash, @FullName, @Phone, @RoleId);
        SET @NewUserId = SCOPE_IDENTITY();
    END
    ');
    PRINT 'Процедура RegisterUser создана.';
END
GO

-- =============================================================================
-- 4. ТРИГГЕРЫ АУДИТА
--    Все триггеры записывают изменения в AuditLog в формате JSON.
--    UserId берётся из SESSION_CONTEXT(N'UserId'), который устанавливает
--    SadovodDbContext перед каждым SaveChangesAsync через SetSessionUserContext.
-- =============================================================================

-- -----------------------------------------------
-- 4.1 trg_Products_Insert — аудит создания товара
-- -----------------------------------------------
IF OBJECT_ID('dbo.trg_Products_Insert', 'TR') IS NULL
BEGIN
    EXEC('
    CREATE TRIGGER trg_Products_Insert ON dbo.Products AFTER INSERT
    AS
    BEGIN
        SET NOCOUNT ON;
        DECLARE @UserId INT = TRY_CONVERT(INT, SESSION_CONTEXT(N''UserId''));
        INSERT INTO dbo.AuditLog (TableName, Operation, NewValue, UserId)
        SELECT
            ''Products'',
            ''I'',
            (SELECT i.* FROM inserted i WHERE i.Id = ins.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @UserId
        FROM inserted ins;
    END
    ');
    PRINT 'Триггер trg_Products_Insert создан.';
END
GO

-- -----------------------------------------------
-- 4.2 trg_Products_Update — аудит изменения товара
-- -----------------------------------------------
IF OBJECT_ID('dbo.trg_Products_Update', 'TR') IS NULL
BEGIN
    EXEC('
    CREATE TRIGGER trg_Products_Update ON dbo.Products AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        DECLARE @UserId INT = TRY_CONVERT(INT, SESSION_CONTEXT(N''UserId''));
        INSERT INTO dbo.AuditLog (TableName, Operation, OldValue, NewValue, UserId)
        SELECT
            ''Products'',
            ''U'',
            (SELECT d.* FROM deleted  d WHERE d.Id = i.Id   FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT i.* FROM inserted i WHERE i.Id = del.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @UserId
        FROM deleted del
        INNER JOIN inserted i ON del.Id = i.Id;
    END
    ');
    PRINT 'Триггер trg_Products_Update создан.';
END
GO

-- -----------------------------------------------
-- 4.3 trg_Products_Delete — аудит удаления товара
-- -----------------------------------------------
IF OBJECT_ID('dbo.trg_Products_Delete', 'TR') IS NULL
BEGIN
    EXEC('
    CREATE TRIGGER trg_Products_Delete ON dbo.Products AFTER DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        DECLARE @UserId INT = TRY_CONVERT(INT, SESSION_CONTEXT(N''UserId''));
        INSERT INTO dbo.AuditLog (TableName, Operation, OldValue, UserId)
        SELECT
            ''Products'',
            ''D'',
            (SELECT d.* FROM deleted d WHERE d.Id = del.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @UserId
        FROM deleted del;
    END
    ');
    PRINT 'Триггер trg_Products_Delete создан.';
END
GO

-- -----------------------------------------------
-- 4.4 trg_Users_Update — аудит изменения профиля пользователя
--     Срабатывает при изменении ФИО, телефона ИЛИ роли (RoleId).
--     Пароль и Login намеренно не логируются.
--     CREATE OR ALTER — безопасно перезапускать, обновляет существующий триггер.
-- -----------------------------------------------
CREATE OR ALTER TRIGGER trg_Users_Update ON dbo.Users AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Реагируем на изменения ФИО, телефона или роли
    IF NOT UPDATE(FullName) AND NOT UPDATE(Phone) AND NOT UPDATE(RoleId) RETURN;

    DECLARE @UserId INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserId'));

    INSERT INTO dbo.AuditLog (TableName, Operation, OldValue, NewValue, UserId)
    SELECT
        'Users',
        'U',
        (SELECT d.Id, d.FullName, d.Phone, d.RoleId
         FROM deleted d WHERE d.Id = i.Id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
        (SELECT i.Id, i.FullName, i.Phone, i.RoleId
         FROM inserted i WHERE i.Id = del.Id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
        @UserId
    FROM deleted del
    INNER JOIN inserted i ON del.Id = i.Id
    WHERE ISNULL(del.FullName, '') <> ISNULL(i.FullName, '')
       OR ISNULL(del.Phone,    '') <> ISNULL(i.Phone,    '')
       OR del.RoleId <> i.RoleId;
END
GO

-- =============================================================================
-- 5. НАЧАЛЬНЫЕ ДАННЫЕ (SEED)
--    Вставляются только если таблица пуста — безопасно запускать повторно.
--    Приложение также выполняет seed при старте через SeedService.cs.
-- =============================================================================

-- -----------------------------------------------
-- 5.1 Роли
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    SET IDENTITY_INSERT dbo.Roles ON;
    INSERT INTO dbo.Roles (Id, Name) VALUES
        (1, N'Администратор'),
        (2, N'Пользователь');
    SET IDENTITY_INSERT dbo.Roles OFF;
    PRINT 'Seed: Roles заполнены.';
END
GO

-- -----------------------------------------------
-- 5.2 Статусы заказов
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.OrderStatuses)
BEGIN
    SET IDENTITY_INSERT dbo.OrderStatuses ON;
    INSERT INTO dbo.OrderStatuses (Id, Name) VALUES
        (1, N'Новый'),
        (2, N'Обработан'),
        (3, N'Выполнен'),
        (4, N'Отменён');
    SET IDENTITY_INSERT dbo.OrderStatuses OFF;
    PRINT 'Seed: OrderStatuses заполнены.';
END
GO

-- -----------------------------------------------
-- 5.3 Категории товаров
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Name) VALUES
        (N'Семена овощей'),
        (N'Семена цветов'),
        (N'Удобрения'),
        (N'Инструменты');
    PRINT 'Seed: Categories заполнены.';
END
GO

-- -----------------------------------------------
-- 5.4 Настройки магазина
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.ShopSettings)
BEGIN
    INSERT INTO dbo.ShopSettings (Email, Phone, Address, AboutUs) VALUES (
        N'info@sadovod.ru',
        N'+79990001122',
        N'г. Москва, ул. Зелёная, д.1',
        N'Интернет-магазин «Садовод» — это команда увлечённых садоводов, агрономов и предпринимателей, ' +
        N'которая с 2008 года помогает дачникам и фермерам по всей России собирать богатый урожай.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10) +
        N'В нашем каталоге — более 3 000 позиций: классические и редкие сорта овощей, ароматные пряные травы, ' +
        N'эффектные однолетники и многолетники, профессиональные удобрения и надёжный садовый инвентарь.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10) +
        N'Главная наша ценность — доверие покупателей. Мы даём гарантию всхожести семян, бережно упаковываем ' +
        N'каждый заказ и доставляем его по всей России.' + CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10) +
        N'С «Садоводом» вы получаете не просто пакетик семян — вы получаете уверенность, что грядки будут радовать урожаем.'
    );
    PRINT 'Seed: ShopSettings заполнены.';
END
GO

-- -----------------------------------------------
-- 5.5 Товары-примеры
--     Вставляются только если Products пуста.
--     Изображение — заглушка /images/products/no-photo.png.
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    DECLARE @CatOvoshi  INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Семена овощей');
    DECLARE @CatTsvety  INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Семена цветов');
    DECLARE @CatUdob    INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Удобрения');
    DECLARE @CatInstr   INT = (SELECT Id FROM dbo.Categories WHERE Name = N'Инструменты');
    DECLARE @Img        NVARCHAR(500) = N'/images/products/no-photo.png';

    INSERT INTO dbo.Products (Name, Description, Price, Quantity, CategoryId, Variety, ImageUrl) VALUES
        (N'Томат «Бычье сердце»',   N'Крупноплодный салатный сорт, плоды до 600 г, мясистая мякоть.',         59.00,  120, @CatOvoshi, N'Среднеспелый',     @Img),
        (N'Огурец «Зозуля F1»',     N'Партенокарпический гибрид для теплиц и открытого грунта.',               45.00,  200, @CatOvoshi, N'Раннеспелый',      @Img),
        (N'Морковь «Нантская 4»',   N'Сладкая морковь длиной 15-17 см, отлично хранится.',                     35.00,  350, @CatOvoshi, N'Среднеспелый',     @Img),
        (N'Петуния «Каскад»',       N'Ампельная петуния для балконов и подвесных кашпо, обильное цветение.',   79.00,   80, @CatTsvety, N'Махровая',         @Img),
        (N'Бархатцы «Кармен»',      N'Низкорослые бархатцы, цветут до заморозков, неприхотливы.',              39.00,  220, @CatTsvety, N'Низкорослые',      @Img),
        (N'Удобрение «Фертика Люкс»',N'Универсальное минеральное удобрение для овощей и цветов, 1 кг.',        320.00,  60, @CatUdob,   N'Минеральное',      @Img),
        (N'Секатор «Садовод Pro»',  N'Профессиональный секатор с тефлоновым покрытием лезвий.',               890.00,   25, @CatInstr,  N'Профессиональный', @Img),
        (N'Лейка садовая 10 л',     N'Пластиковая лейка с насадкой-распылителем, морозостойкая.',             450.00,   40, @CatInstr,  N'Бытовая',          @Img);

    PRINT 'Seed: Products заполнены (8 товаров).';
END
GO

-- =============================================================================
-- ГОТОВО
-- =============================================================================
PRINT '';
PRINT '=================================================================';
PRINT ' База данных SadovodShop успешно инициализирована.';
PRINT ' Версия скрипта: 3.0';
PRINT '=================================================================';
GO
