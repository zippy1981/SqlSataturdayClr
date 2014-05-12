-- DEMO 1: 2 SQLCLR UDFs
DECLARE @emails TABLE (
	id INT NOT NULL IDENTITY(1,1),
	email NVARCHAR(50) NOT NULL PRIMARY KEY NONCLUSTERED
)
INSERT INTO @emails (email) VALUES
	('zippy1981@gmail.com'),
	('Justin Dearing'),
	('gates@microsoft.comn'),
	('@microsoft.com'),
	('zippy1981@'),
	('zippy1981@gmail')

SELECT id, email, dbo.IsValidEmail(email) AS IsValidEmail FROM @emails ORDER BY id

DECLARE @fruits TABLE (
	id INT NOT NULL IDENTITY(1,1),
	fruit NVARCHAR(50) NOT NULL PRIMARY KEY NONCLUSTERED
)
INSERT INTO @fruits VALUES
	('Apples'),
	('Banannas'),
	('Blueberries'),
	('Kiwis'),
	('Steak')

DECLARE @pattern NVARCHAR(MAX) = '(Apples|Banannas)'
SELECT id, fruit, dbo.Matches(fruit,@pattern) AS IsAppleOrBananna FROM @fruits ORDER BY id
GO
-- DEMO 1 END

-- DEMO 2
IF EXISTS(SELECT name FROM sys.databases WHERE name = 'AdventureWorks')
BEGIN
	RAISERROR('DROPPING AdventureWorks', 10, 1)
	DROP DATABASE [AdventureWorks]
END

EXEC RestoreToDefaultLocation
	@dbName = 'AdventureWorks',
	@path = 'AdventureWorks2008R2-Full Database Backup.bak'

SELECT name, physical_name FROM [AdventureWorks].sys.database_files
GO

-- DEMO 2 END



/*
SELECT * FROM sys.assemblies
SELECT name FROM sys.procedures

EXEC SP_help 'ProcMonDebugOutput'
*/
EXEC ProcMonDebugOutput 'Inside SqlClr Duh'

-- SELECT * FROM sys.dm_os_memory_objects where type like '%clr%'
-- SELECT * FROM sys.dm_clr_appdomains


SELECT 'zippy1981@gmail.com' as email, dbo.IsValidEmail('zippy1981@gmail.com') as IsValidEmail
UNION
SELECT 'zippy1981!gmail.com', dbo.IsValidEmail('zippy1981!gmail.com')

SELECT 'Apples' as Fruit, dbo.Matches('Apples', '(Apples|Banannas)') AS AppleOrBananna
UNION
SELECT 'Banannas' as Fruit, dbo.Matches('Banannas', '(Apples|Banannas)') AS AppleOrBananna
UNION
SELECT 'Blueberries' as Fruit, dbo.Matches('BLueberries', '(Apples|Banannas)') AS AppleOrBananna
