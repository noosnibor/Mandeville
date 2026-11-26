/* ============================================================================
   Clear data (WAFIF Mandeville)
   Removes the information people entered, and keeps the setup so the app still works.

   KEPT:    sign-in accounts (their member link is cleared), roles and permissions (tblAppUser, tblRole, tblRolePermission),
            Setup lists (member types, departments, expense categories, payment types,
            branches, lookup values, message templates).
   DELETED: members and their photos, visitors, visiting members, attendance, services,
            collections, expenditure, reconciliations, ledger periods, pastoral care,
            evangelism and celebration records.

   HOW TO USE
     1. Back up first (Azure portal > the database > Export, or a copy of the database).
     2. Run the script once as it is. It lists what WOULD be deleted and changes nothing.
     3. Set @ConfirmDelete = 1 and run it again to delete. It is one transaction, so an
        error undoes everything.
   Optional parts at the bottom (clear Setup lists, clear accounts) are commented out.
   ============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ConfirmDelete BIT = 0;      -- 0 = only show counts, 1 = really delete

DECLARE @tables TABLE (fstrTable SYSNAME NOT NULL);
INSERT INTO @tables (fstrTable) VALUES
    -- attendance and services
    (N'tblMemberAttendance'), (N'tblVisitorAttendance'), (N'tblVisitingAttendance'),
    (N'tblServiceAssignment'), (N'tblService'),
    -- finance
    (N'tblReconciliationItem'), (N'tblReconciliation'), (N'tblLedgerPeriod'),
    (N'tblFinance'), (N'tblExpenditure'),
    -- pastoral care and evangelism
    (N'tblPastoralContact'), (N'tblPastoralAssignment'),
    (N'tblEvangelismAttempt'), (N'tblEvangelismInvite'), (N'tblEvangelismContact'),
    (N'tblEvangelismAvailability'), (N'tblEvangelismAssignment'), (N'tblEvangelismService'),
    -- celebrations
    (N'tblCelebrationQueue'), (N'tblCelebrationLog'),
    -- people
    (N'tblVisitorNote'), (N'tblVisitor'), (N'tblVisitingMember'),
    (N'tblMemberDepartment'), (N'tblMemberPhoto'), (N'tblMember');

-- Optional: also clear the Setup lists (remove the two dashes at the start of the lines to use)
-- INSERT INTO @tables (fstrTable) VALUES
--     (N'tblMemberType'), (N'tblDepartment'), (N'tblExpenseCategory'), (N'tblPaymentType'),
--     (N'tblBranch'), (N'tblLookupValue'), (N'tblMessageTemplate');

-- Optional: also clear sign-in accounts. You will not be able to sign in until you add one again.
-- INSERT INTO @tables (fstrTable) VALUES (N'tblAppUser'), (N'tblRolePermission'), (N'tblRole');

DECLARE @name SYSNAME, @sql NVARCHAR(400), @rows INT, @total INT = 0;

BEGIN TRAN;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT fstrTable FROM @tables;
OPEN c;
FETCH NEXT FROM c INTO @name;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'mandeville.' + @name, N'U') IS NULL
        PRINT N'Skipped (table not found): ' + @name;
    ELSE
    BEGIN
        SET @sql = N'DELETE FROM mandeville.' + QUOTENAME(@name) + N';';
        IF @ConfirmDelete = 1
        BEGIN
            EXEC sp_executesql @sql;
            SET @rows = @@ROWCOUNT;
            PRINT N'Deleted ' + CAST(@rows AS NVARCHAR(12)) + N' rows from ' + @name;
        END
        ELSE
        BEGIN
            SET @sql = N'SELECT @n = COUNT(*) FROM mandeville.' + QUOTENAME(@name) + N';';
            EXEC sp_executesql @sql, N'@n INT OUTPUT', @n = @rows OUTPUT;
            PRINT N'Would delete ' + CAST(@rows AS NVARCHAR(12)) + N' rows from ' + @name;
        END
        SET @total += @rows;
    END
    FETCH NEXT FROM c INTO @name;
END
CLOSE c; DEALLOCATE c;

-- Kept sign-in accounts may point at a member that no longer exists: unlink them (the accounts stay).
IF OBJECT_ID(N'mandeville.tblAppUser', N'U') IS NOT NULL
BEGIN
    IF @ConfirmDelete = 1
    BEGIN
        UPDATE mandeville.tblAppUser SET flngMemberKey = NULL WHERE flngMemberKey IS NOT NULL;
        PRINT N'Unlinked ' + CAST(@@ROWCOUNT AS NVARCHAR(12)) + N' sign-in accounts from their member record.';
    END
    ELSE
    BEGIN
        SELECT @rows = COUNT(*) FROM mandeville.tblAppUser WHERE flngMemberKey IS NOT NULL;
        PRINT N'Would unlink ' + CAST(@rows AS NVARCHAR(12)) + N' sign-in accounts from their member record.';
    END
END

IF @ConfirmDelete = 1
BEGIN
    COMMIT TRAN;
    PRINT N'Done. ' + CAST(@total AS NVARCHAR(12)) + N' rows deleted.';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT N'Nothing was changed. ' + CAST(@total AS NVARCHAR(12)) + N' rows would be deleted. Set @ConfirmDelete = 1 to delete.';
END
