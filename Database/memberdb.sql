USE [master]
GO
/****** Object:  Database [BOMBER_MEMBER]    Script Date: 11/19/2020 9:57:50 PM ******/
CREATE DATABASE [BOMBER_MEMBER]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'MEMBER', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\MEMBER.mdf' , SIZE = 4096KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'MEMBER_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\MEMBER_log.ldf' , SIZE = 136064KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO
ALTER DATABASE [BOMBER_MEMBER] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [BOMBER_MEMBER].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [BOMBER_MEMBER] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET ARITHABORT OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [BOMBER_MEMBER] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [BOMBER_MEMBER] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET  DISABLE_BROKER 
GO
ALTER DATABASE [BOMBER_MEMBER] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [BOMBER_MEMBER] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET RECOVERY FULL 
GO
ALTER DATABASE [BOMBER_MEMBER] SET  MULTI_USER 
GO
ALTER DATABASE [BOMBER_MEMBER] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [BOMBER_MEMBER] SET DB_CHAINING OFF 
GO
ALTER DATABASE [BOMBER_MEMBER] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [BOMBER_MEMBER] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [BOMBER_MEMBER] SET DELAYED_DURABILITY = DISABLED 
GO
EXEC sys.sp_db_vardecimal_storage_format N'BOMBER_MEMBER', N'ON'
GO
ALTER DATABASE [BOMBER_MEMBER] SET QUERY_STORE = OFF
GO
USE [BOMBER_MEMBER]
GO
/****** Object:  User [bomberuser]    Script Date: 11/19/2020 9:57:50 PM ******/
CREATE USER [bomberuser] FOR LOGIN [bomberuser] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [bomberuser]
GO
/****** Object:  Table [dbo].[tbl_ServerList]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_ServerList](
	[serverId] [int] NOT NULL,
	[registerDate] [datetime] NOT NULL,
	[name] [nvarchar](32) NOT NULL,
	[serverIP] [varchar](15) NOT NULL,
	[serverPort] [int] NOT NULL,
	[currentUserCount] [int] NOT NULL,
	[maxUserCount] [int] NOT NULL,
	[status] [int] NOT NULL,
	[isNew] [bit] NOT NULL,
	[isMaintenance] [bit] NOT NULL,
	[isDeleted] [bit] NOT NULL,
	[deleteDate] [datetime] NOT NULL,
 CONSTRAINT [PK_tbl_ServerList] PRIMARY KEY NONCLUSTERED 
(
	[serverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_User]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_User](
	[userId] [int] IDENTITY(1,1) NOT NULL,
	[registerDate] [datetime] NOT NULL,
	[username] [nvarchar](32) NOT NULL,
	[password] [nvarchar](32) NOT NULL,
	[sessionKey] [varchar](128) NULL,
	[auth] [tinyint] NOT NULL,
	[lastIp] [nvarchar](15) NOT NULL,
	[lastLogin] [datetime] NOT NULL,
	[lastServer] [int] NOT NULL,
 CONSTRAINT [PK_tbl_User] PRIMARY KEY CLUSTERED 
(
	[userId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_UserCash]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_UserCash](
	[userCashId] [int] IDENTITY(1,1) NOT NULL,
	[userId] [int] NOT NULL,
	[registerDate] [datetime] NOT NULL,
	[cash] [int] NOT NULL,
 CONSTRAINT [PK_tbl_UserCash] PRIMARY KEY CLUSTERED 
(
	[userCashId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_UserVip]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_UserVip](
	[userVipId] [int] IDENTITY(1,1) NOT NULL,
	[userId] [int] NOT NULL,
	[registerDate] [datetime] NOT NULL,
	[vipLevel] [int] NOT NULL,
	[vipPoint] [int] NOT NULL,
 CONSTRAINT [PK_tbl_UserVip] PRIMARY KEY CLUSTERED 
(
	[userVipId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_registerDate]  DEFAULT (getdate()) FOR [registerDate]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_name]  DEFAULT ('NO_NAME') FOR [name]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_serverIP]  DEFAULT ('0.0.0.0') FOR [serverIP]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_serverPort]  DEFAULT ((0)) FOR [serverPort]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_currentUserCount]  DEFAULT ((0)) FOR [currentUserCount]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_maxUserCount]  DEFAULT ((0)) FOR [maxUserCount]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_status]  DEFAULT ((0)) FOR [status]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_isNew]  DEFAULT ((1)) FOR [isNew]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_isMaintenance]  DEFAULT ((0)) FOR [isMaintenance]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_isDeleted]  DEFAULT ((0)) FOR [isDeleted]
GO
ALTER TABLE [dbo].[tbl_ServerList] ADD  CONSTRAINT [DF_tbl_ServerList_deleteDate]  DEFAULT ((0)) FOR [deleteDate]
GO
ALTER TABLE [dbo].[tbl_User] ADD  CONSTRAINT [DF_tbl_User_registerDate]  DEFAULT (getdate()) FOR [registerDate]
GO
ALTER TABLE [dbo].[tbl_User] ADD  CONSTRAINT [DF_tbl_User_auth]  DEFAULT ((0)) FOR [auth]
GO
ALTER TABLE [dbo].[tbl_User] ADD  CONSTRAINT [DF_tbl_User_lastIp]  DEFAULT (N'0.0.0.0') FOR [lastIp]
GO
ALTER TABLE [dbo].[tbl_User] ADD  CONSTRAINT [DF_tbl_User_lastLogin]  DEFAULT (getdate()) FOR [lastLogin]
GO
ALTER TABLE [dbo].[tbl_User] ADD  CONSTRAINT [DF_tbl_User_lastServer]  DEFAULT ((0)) FOR [lastServer]
GO
ALTER TABLE [dbo].[tbl_UserCash] ADD  CONSTRAINT [DF_tbl_UserCash_registerDate]  DEFAULT (getdate()) FOR [registerDate]
GO
ALTER TABLE [dbo].[tbl_UserCash] ADD  CONSTRAINT [DF_tbl_UserCash_cash]  DEFAULT ((0)) FOR [cash]
GO
ALTER TABLE [dbo].[tbl_UserVip] ADD  CONSTRAINT [DF_tbl_UserVip_registerDate]  DEFAULT (getdate()) FOR [registerDate]
GO
ALTER TABLE [dbo].[tbl_UserVip] ADD  CONSTRAINT [DF_tbl_UserVip_vipLevel]  DEFAULT ((0)) FOR [vipLevel]
GO
ALTER TABLE [dbo].[tbl_UserVip] ADD  CONSTRAINT [DF_tbl_UserVip_vipPoint]  DEFAULT ((0)) FOR [vipPoint]
GO
/****** Object:  StoredProcedure [dbo].[usp_GetAllServerList]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*=================================================================================================
  Description	: Get all server's from server list.
  Creator		: Julio Litwin
=================================================================================================*/
CREATE PROCEDURE [dbo].[usp_GetAllServerList]
AS
BEGIN

	SET NOCOUNT ON
  
	SELECT *
		FROM tbl_ServerList WITH(NOLOCK)
	WHERE isDeleted = 0
END
GO
/****** Object:  StoredProcedure [dbo].[usp_GetUserCash]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ================================================================================
-- Description	: Get User cash by user Id.
-- Creator		: Julio Litwin
-- ================================================================================
CREATE PROCEDURE [dbo].[usp_GetUserCash]
	@userId int
AS
BEGIN
	SET NOCOUNT ON
	
	SELECT *
	  FROM tbl_UserCash WITH (NOLOCK)
	 WHERE userId = @userId
END
GO
/****** Object:  StoredProcedure [dbo].[usp_GetUserVip]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ================================================================================
-- Description	: Get User Vip by user Id.
-- Creator		: Julio Litwin
-- ================================================================================
CREATE PROCEDURE [dbo].[usp_GetUserVip]
	@userId int
AS
BEGIN
	SET NOCOUNT ON
	
	SELECT *
	  FROM tbl_UserVip WITH (NOLOCK)
	 WHERE userId = @userId
END
GO
/****** Object:  StoredProcedure [dbo].[usp_UpdateServerList]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ================================================================================
-- Description	: Update server list
-- Creator		: Julio Litwin
-- ================================================================================
CREATE PROCEDURE [dbo].[usp_UpdateServerList]
	@serverId			int,
	@status				int,
	@currentUserCount	int
AS
BEGIN
	SET NOCOUNT ON
	
	UPDATE tbl_ServerList
	   SET currentUserCount = @currentUserCount,
		   status = @status
	 WHERE serverId = @serverId
	
	IF @@ERROR <> 0 OR @@ROWCOUNT <> 1
		RETURN 1
	
	RETURN 0
END





GO
/****** Object:  StoredProcedure [dbo].[usp_UpdateUserLogin]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/*=================================================================================================
  Description	: Edit user-login
  Author		: Julio Litwin
=================================================================================================*/
CREATE PROCEDURE [dbo].[usp_UpdateUserLogin]
	@sUserId			int,
	@bSessionKey		varchar(128),
	@dtoLastLoginTime	datetime,
	@sLastLoginIp		nvarchar(15)
AS
BEGIN
	SET NOCOUNT ON

	UPDATE tbl_User
	   SET sessionKey		= @bSessionKey,
	       lastLogin		= @dtoLastLoginTime,
	       lastIp			= @sLastLoginIp
	 WHERE userId = @sUserId
	   --AND deleted = 0
	
	IF @@ERROR <> 0 OR @@ROWCOUNT <> 1
		RETURN 1
	
	RETURN 0
END





GO
/****** Object:  StoredProcedure [dbo].[usp_User]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ================================================================================
-- Description	: Get User
-- Creator		: Julio Litwin
-- ================================================================================
CREATE PROCEDURE [dbo].[usp_User]
	@username nvarchar(32)
AS
BEGIN
	SET NOCOUNT ON
	
	SELECT *
	  FROM tbl_User WITH (NOLOCK)
	 WHERE username = @username
END
GO
/****** Object:  StoredProcedure [dbo].[usp_UserByID]    Script Date: 11/19/2020 9:57:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ================================================================================
-- Description	: Get User by ID.
-- Creator		: Julio Litwin
-- ================================================================================
CREATE PROCEDURE [dbo].[usp_UserByID]
	@userId int
AS
BEGIN
	SET NOCOUNT ON
	
	SELECT *
	  FROM tbl_User WITH (NOLOCK)
	 WHERE userId = @userId
END
GO
USE [master]
GO
ALTER DATABASE [BOMBER_MEMBER] SET  READ_WRITE 
GO
