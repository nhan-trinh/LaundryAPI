# 31 — Thu tiền và trả toàn bộ đồ

Status: resolved
Type: task
Blocked by: 30

Nhân viên thu đủ bằng tiền mặt hoặc chuyển khoản và bàn giao toàn bộ đồ từ một phiếu sẵn sàng trả.

## Tiêu chí nghiệm thu

- [x] Endpoint bàn giao nhận phương thức tiền mặt hoặc chuyển khoản. Với mỗi phương thức, đọc lại phiếu đã trả thấy đúng một lần thu đủ tổng tiền và lịch sử ghi người cùng thời điểm bàn giao.
- [x] Thanh toán và chuyển sang Đã trả cùng thành công trong một giao dịch. Nếu thao tác thất bại giữa chừng, phiếu vẫn sẵn sàng trả và chưa ghi nhận khoản thu.
- [x] Bàn giao khi sai trạng thái hoặc dùng phiên bản cũ trả 409 với Problem Details. Hai yêu cầu cùng bàn giao một phiên bản chỉ ghi một khoản thu và một sự kiện Đã trả.
- [x] Đã trả là trạng thái cuối. Gửi lại yêu cầu sau khi bàn giao không tạo thêm khoản thu.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Answer

Staff hoặc Manager gửi `POST /api/v1/orders/{code}/handover` với `version` của phiếu sẵn sàng trả và `paymentMethod` là `cash` hoặc `bankTransfer`. Khi nhận 204, đọc lại phiếu để xem trạng thái `Returned`, khoản thu trong `payment` và người bàn giao cùng thời điểm trong `history`. API tính số tiền từ các dòng trên phiếu.

Kiểm thử HTTP trên SQL Server xác nhận hai yêu cầu cùng phiên bản nhận một phản hồi 204 và một phản hồi 409. Khi SQL Server từ chối ghi sự kiện bàn giao, đọc lại phiếu vẫn thấy `Ready`, phiên bản cũ và `payment` bằng `null`. [Kiểm thử phiếu giặt](../../../tests/LaundryAPI.Tests/OrderTests.cs)
