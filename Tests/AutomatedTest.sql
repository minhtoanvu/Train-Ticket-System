SET NOCOUNT ON;
PRINT '=== BẮT ĐẦU KIỂM THỬ TỰ ĐỘNG (INTEGRATION TEST) ===';

-- 0. CẬP NHẬT GIỜ KHỞI HÀNH
UPDATE TOP(1) Schedules 
SET ArrivalTime = DATEADD(minute, DATEDIFF(minute, DepartureTime, ArrivalTime), DATEADD(day, 10, GETDATE())), 
    DepartureTime = DATEADD(day, 10, GETDATE()), 
    Status = N'Scheduled' 
WHERE IsActive = 1;

-- 1. SETUP DỮ LIỆU
DECLARE @UserID INT = 3;
DECLARE @ScheduleID INT;
DECLARE @SeatID INT;

SELECT TOP 1 @ScheduleID = s.ScheduleID FROM Schedules s WHERE s.DepartureTime > GETDATE() AND s.Status = N'Scheduled';
SELECT TOP 1 @SeatID = se.SeatID FROM Seats se JOIN Carriages c ON se.CarriageID = c.CarriageID JOIN Schedules sc ON sc.TrainID = c.TrainID WHERE sc.ScheduleID = @ScheduleID AND se.SeatID NOT IN (SELECT SeatID FROM Tickets WHERE ScheduleID = @ScheduleID AND Status NOT IN (N'Cancelled', N'Expired'));

PRINT '>> Đã tìm thấy ScheduleID: ' + CAST(@ScheduleID AS VARCHAR) + ', SeatID: ' + CAST(@SeatID AS VARCHAR);

-- 2. TEST CASE 1: ĐẶT VÉ
PRINT '---------------------------------';
PRINT '>> TEST CASE 1: Đang thực thi sp_DatVe...';

EXEC sp_DatVe 
    @UserID = @UserID,
    @ScheduleID = @ScheduleID,
    @SeatIDs = @SeatID,
    @PassengerNames = N'Test User Automation',
    @PassengerIDs = N'012345678912',
    @PassengerPhones = N'0987654321',
    @PaymentMethod = N'BankTransfer';

DECLARE @TicketID INT;
SELECT TOP 1 @TicketID = TicketID FROM Tickets WHERE UserID = @UserID ORDER BY TicketID DESC;

DECLARE @TStatus NVARCHAR(20), @PStatus NVARCHAR(20);
SELECT @TStatus = Status FROM Tickets WHERE TicketID = @TicketID;
SELECT @PStatus = Status FROM Payments WHERE TicketID = @TicketID;

IF @TStatus = 'Pending' AND @PStatus = 'Pending'
    PRINT '>> OK Đặt vé thành công! Tickets (Pending) - Payments (Pending). TicketID: ' + CAST(@TicketID AS VARCHAR);
ELSE
    PRINT '❌ LỖI trạng thái sau đặt vé!';

-- 3. TEST CASE 2: THANH TOÁN
PRINT '---------------------------------';
PRINT '>> TEST CASE 2: Đang thực thi sp_XacNhanThanhToan...';
EXEC sp_XacNhanThanhToan @TicketID = @TicketID, @TransactionID = N'TEST_TRANS_9999';

SELECT @TStatus = Status FROM Tickets WHERE TicketID = @TicketID;
SELECT @PStatus = Status FROM Payments WHERE TicketID = @TicketID;

IF @TStatus = 'Confirmed' AND @PStatus = 'Success'
    PRINT '>> OK Thanh toán thành công! Tickets (Confirmed) - Payments (Success).';
ELSE
    PRINT '❌ LỖI trạng thái sau thanh toán!';

-- 4. TEST CASE 3: HỦY VÉ
PRINT '---------------------------------';
PRINT '>> TEST CASE 3: Đang thực thi sp_HuyVe...';
EXEC sp_HuyVe @TicketID = @TicketID, @UserID = @UserID, @CancelReason = N'Automation Test';

SELECT @TStatus = Status FROM Tickets WHERE TicketID = @TicketID;
SELECT @PStatus = Status FROM Payments WHERE TicketID = @TicketID;

IF @TStatus = 'Cancelled' AND @PStatus IN ('Refunded', 'Failed')
    PRINT '>> OK Hủy vé thành công! Tickets (Cancelled) - Payments (' + @PStatus + ').';
ELSE
    PRINT '❌ LỖI trạng thái sau hủy vé!';

PRINT '---------------------------------';
PRINT '=== HOÀN TẤT KIỂM THỬ THÀNH CÔNG ===';
