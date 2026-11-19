# 29 — Sửa hoặc hủy phiếu khi vừa nhận

Status: resolved
Type: task
Blocked by: 28

Nhân viên sửa các dòng khi tiếp nhận hoặc hủy phiếu có lý do để cửa hàng tra cứu về sau.

## Tiêu chí nghiệm thu

- [x] Sửa lượng trên phiếu `Received` và đọc lại thấy tổng tiền tính lại theo đơn giá đã lưu. Các dòng sau khi sửa vẫn đáp ứng quy tắc số lượng của phiếu.
- [x] Endpoint hủy riêng chỉ chấp nhận phiếu `Received` và lý do có nội dung. Khi thiếu lý do, trả 400 với Problem Details và phiếu vẫn ở `Received`.
- [x] Hủy thành công chuyển sang `Cancelled`, lưu `CancellationReason`, `CancelledAt` và `CancelledBy` theo tài khoản đã xác thực. Chi tiết phiếu giữ nguyên toàn bộ dòng và hiển thị lịch sử hủy.
- [x] Phiếu đã hủy kết thúc vòng đời, không thể sửa dòng hoặc khôi phục. Thao tác xung đột với trạng thái hoặc dùng phiên bản cũ trả 409 với Problem Details, giữ nguyên kết quả đã ghi.
- [x] Khi cả ticket 29 và 30 đã có mặt, kiểm tra hai yêu cầu hủy và bắt đầu xử lý cùng phiên bản. Đúng một yêu cầu thành công, yêu cầu kia nhận 409, lịch sử chỉ ghi bước đã thành công.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Answer

Staff và Manager sửa lượng bằng `PUT /api/v1/orders/{code}/lines/{lineId}/quantity` với `version` và `quantity`, hoặc hủy bằng `POST /api/v1/orders/{code}/cancel` với `version` và `cancellationReason`. Dùng phiên bản Base64 trong chi tiết phiếu. Sau khi nhận 204, đọc lại phiếu để lấy phiên bản mới.

Kiểm thử HTTP trên SQL Server xác nhận đơn vị và giá đã lưu quyết định tổng tiền sau khi sửa. Hai yêu cầu hủy đồng thời cho cùng phiên bản trả một 204 và một 409, giữ nguyên các dòng và chỉ ghi lý do, tài khoản, thời điểm của lần hủy thành công. Phép thử hủy đồng thời với bắt đầu xử lý đã đạt trong ticket 30. [Kiểm thử phiếu giặt](../../../tests/LaundryAPI.Tests/OrderTests.cs)
