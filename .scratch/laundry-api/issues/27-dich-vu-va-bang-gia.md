# 27 — Quản lý dịch vụ và bảng giá

Status: resolved
Type: task
Blocked by: 24

Manager quản lý dịch vụ tính theo kg hoặc chiếc. Nhân viên đọc danh mục đang cung cấp để báo giá tại quầy.

## Tiêu chí nghiệm thu

- [x] Manager tạo và cập nhật dịch vụ với tên, đơn vị tính và đơn giá VND. Đọc lại thấy thông tin mới.
- [x] Staff và Manager xem được các dịch vụ đang cung cấp cùng đơn vị và giá. Chỉ Manager được thay đổi danh mục hoặc bảng giá.
- [x] Khi Manager ngừng cung cấp một dịch vụ, dịch vụ đó biến mất khỏi danh sách chọn cho dòng mới.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Answer

Staff và Manager đọc danh mục đang cung cấp qua `GET /api/v1/services`. Manager tạo bằng `POST /api/v1/services` và cập nhật bằng `PUT /api/v1/services/{id}`. Cả hai thao tác nhận `name`, `unit` (`kg` hoặc `piece`, tức chiếc) và `unitPriceVnd` (số đồng nguyên từ 0 trở lên).

Manager ngừng cung cấp bằng `POST /api/v1/services/{id}/discontinue`. Bản ghi được giữ trong SQL Server và dịch vụ được ẩn khỏi danh sách chọn tại quầy. Yêu cầu cập nhật dịch vụ bằng ID cũ trả HTTP 404. Ba kiểm thử HTTP đã xác nhận thay đổi giá, phân quyền, ngừng cung cấp và từ chối dữ liệu không hợp lệ sau khi áp dụng migration `ServiceCatalog`. [Kiểm thử danh mục dịch vụ](../../../tests/LaundryAPI.Tests/ServiceCatalogTests.cs)
