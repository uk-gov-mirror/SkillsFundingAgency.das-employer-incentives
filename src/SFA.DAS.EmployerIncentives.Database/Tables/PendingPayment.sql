CREATE TABLE [incentives].[PendingPayment]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY NONCLUSTERED,	
    [AccountId] BIGINT NOT NULL, 
    [ApprenticeshipIncentiveId] UNIQUEIDENTIFIER NOT NULL,
    [DueDate] DATETIME2 NOT NULL, 
    [Amount] DECIMAL(9, 2) NOT NULL, 
    [CalculatedDate] DATETIME2 NOT NULL, 
    [PaymentMadeDate] DATETIME2 NULL,
    [PeriodNumber] TINYINT NULL,
	[PaymentYear] SMALLINT NULL,
    [AccountLegalEntityId] BIGINT NULL,
    [EarningType] VARCHAR(20) NULL
    CONSTRAINT FK_ApprenticeshipIncentive FOREIGN KEY (ApprenticeshipIncentiveId) REFERENCES [incentives].[ApprenticeshipIncentive](Id), 
    [ClawedBack] BIT NOT NULL DEFAULT 0
)
GO
CREATE CLUSTERED INDEX IX_PendingPayment ON [incentives].[PendingPayment] (AccountId)
GO
CREATE INDEX IX_PendingPayment_DuePayments ON [incentives].[PendingPayment] (PaymentMadeDate, PaymentYear, PeriodNumber) INCLUDE (AccountLegalEntityId)
GO
CREATE INDEX IX_PendingPayment_DuePaymentsForALE ON [incentives].[PendingPayment] (AccountLegalEntityId, PaymentMadeDate, PaymentYear, PeriodNumber) INCLUDE (Id)
GO
CREATE INDEX IX_PendingPayment_ApprenticeshipIncentiveId ON [incentives].[PendingPayment] (ApprenticeshipIncentiveId)