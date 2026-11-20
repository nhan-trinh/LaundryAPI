# Hợp đồng API v1

Status: resolved
Type: grilling
Blocked by: 07, 09, 10, 16

API thể hiện thao tác nghiệp vụ và lỗi thế nào để dễ thử trên Swagger?

## Comments

Đề xuất. Dùng endpoint riêng để bắt đầu xử lý, đánh dấu sẵn sàng, trả đồ kèm thanh toán và hủy. Trả lỗi theo Problem Details, dùng HTTP 400 cho dữ liệu sai và 409 khi thao tác xung đột với trạng thái hoặc phiên bản phiếu.

## Answer

Dùng endpoint riêng để bắt đầu xử lý, đánh dấu sẵn sàng, trả đồ kèm thanh toán và hủy. Trả lỗi theo Problem Details, dùng HTTP 400 cho dữ liệu sai và 409 khi thao tác xung đột với trạng thái hoặc phiên bản phiếu.
