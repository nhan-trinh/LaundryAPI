# 28 — Lập và đọc lại phiếu giặt

Status: resolved
Type: task
Blocked by: 26, 27

Nhân viên tiếp nhận một lần giao đồ, lập phiếu cho một khách và đọc lại phiếu theo mã để đối chiếu tại quầy.

## Tiêu chí nghiệm thu

- [x] Phiếu có mã duy nhất dễ đọc, một khách và ít nhất một dòng dịch vụ đang cung cấp. Cùng phiếu có thể có cả kg và chiếc. Ghi chú từng dòng và thời điểm hẹn trả cả phiếu là tùy chọn.
- [x] Phiếu thiếu khách, thiếu dòng hoặc có lượng không hợp lệ trả 400 với Problem Details. Kg phải dương và có tối đa hai chữ số thập phân. Chiếc phải là số nguyên dương.
- [x] Làm tròn thành tiền mỗi dòng đến đồng gần nhất, nửa đồng làm tròn lên. Phiếu gồm 0,25 kg × 10.002 đồng và 2 chiếc × 30.000 đồng có tổng 62.501 đồng.
- [x] Đổi tên, giá hoặc ngừng dịch vụ sau khi thêm dòng vẫn đọc được tên, đơn vị, đơn giá ban đầu. Tạo dòng mới từ dịch vụ đã ngừng bị từ chối.
- [x] Đọc lại phiếu thấy trạng thái `Received`, phiên bản hiện tại, các dòng, tổng tiền và sự kiện tiếp nhận với người lập và thời điểm.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Answer

Staff và Manager lập phiếu bằng `POST /api/v1/orders` với `customerId`, `lines` và thời điểm hẹn trả tùy chọn `promisedAt`. Mỗi dòng nhận `serviceId`, `quantity` và ghi chú tùy chọn `note`. Phản hồi 201 có mã dạng `PG-123` và đường dẫn đọc lại qua `GET /api/v1/orders/{code}`.

Phiếu lưu tên dịch vụ, đơn vị và đơn giá khi nhận đồ. Chi tiết phiếu hiển thị thành tiền đã làm tròn từng dòng, tổng tiền, trạng thái `Received`, phiên bản và lịch sử tiếp nhận gắn với tài khoản đăng nhập. Các tiêu chí được kiểm tra qua HTTP trên SQL Server sau khi áp dụng migration `LaundryOrders`. [Kiểm thử phiếu giặt](../../../tests/LaundryAPI.Tests/OrderTests.cs)
