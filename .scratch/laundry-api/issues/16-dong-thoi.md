# Hai người cùng thao tác

Status: resolved
Type: grilling
Blocked by: 07, 09, 10

Nếu hai người cùng thao tác trên cùng phiên bản phiếu, xử lý yêu cầu đến sau thế nào?

## Comments

Đề xuất. Chỉ ghi nhận thao tác đầu tiên thành công. Yêu cầu dùng phiên bản cũ nhận HTTP 409 để tải lại phiếu. Thanh toán và bàn giao được lưu trong cùng giao dịch.

## Answer

Chủ dự án đồng ý. Chỉ ghi nhận thao tác đầu tiên thành công. Yêu cầu dùng phiên bản cũ nhận HTTP 409 để tải lại phiếu. Thanh toán và bàn giao được lưu trong cùng giao dịch.
