# Sửa và hủy phiếu

Status: resolved
Type: grilling
Blocked by: 03, 04

## Comments

Phương án cần xác nhận. Cho phép sửa dòng dịch vụ hoặc hủy trước khi bắt đầu xử lý. Giữ phiếu đã hủy và lý do để tra cứu.

## Answer

Chỉ sửa dòng hoặc hủy khi `Received`. Khi hủy phải có lý do, lưu `CancelledAt`, `CancelledBy`, `CancellationReason` và giữ nguyên `Order` cùng `Order Items`. `Cancelled` là trạng thái cuối trong v1. Từ `Processing`, khóa dòng dịch vụ. Mọi điều chỉnh về sau cần quy trình được cấp quyền hoặc cơ chế ghi ngoại lệ có nhật ký.
