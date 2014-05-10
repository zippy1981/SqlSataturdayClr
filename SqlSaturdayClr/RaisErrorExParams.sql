CREATE TYPE [dbo].[RaisErrorExParams] AS TABLE (
	Id INT NOT NULL IDENTITY(0,1),
	Param varchar(max) NOT NULL
);