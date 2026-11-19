# 30 — Bắt đầu xử lý và đánh dấu sẵn sàng trả

Status: resolved
Type: task
Blocked by: 28

Nhân viên theo dõi tiến độ chung của toàn bộ đồ trên phiếu và giữ nguyên các dòng dịch vụ từ khi bắt đầu xử lý.

## Tiêu chí nghiệm thu

- [x] Dùng hai endpoint riêng cho bắt đầu xử lý và đánh dấu sẵn sàng. `Received` chuyển sang `Processing`. Nhân viên xác nhận sẵn sàng trả khi toàn bộ đồ đã xong.
- [x] Đọc lại lịch sử thấy trạng thái mới, thời điểm và người thực hiện từng bước. Bỏ qua bước, lặp bước hoặc dùng phiên bản cũ trả 409 với Problem Details.
- [x] Từ `Processing`, cả Staff và Manager bị khóa sửa dòng. Nếu đã ghi 5 kg, số lượng và tổng tiền khi bàn giao vẫn dựa trên 5 kg.
- [x] Khi tích hợp với ticket 29, kiểm tra sửa hoặc hủy sau khi bắt đầu xử lý đều trả 409. Chạy tình huống hủy đồng thời với bắt đầu xử lý mô tả ở ticket 29.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Answer

Staff và Manager dùng `POST /api/v1/orders/{code}/start-processing` để bắt đầu xử lý phiếu đã nhận. Khi toàn bộ đồ đã xong, dùng `POST /api/v1/orders/{code}/mark-ready` để chuyển phiếu đang xử lý sang `Ready`. Cả hai lệnh nhận `version` từ chi tiết phiếu và trả 204 khi thành công. Đọc lại phiếu để lấy phiên bản mới và xem ai đã xác nhận từng bước, vào lúc nào.

Kiểm thử HTTP trên SQL Server xác nhận phiếu 5 kg giữ tổng 50.010 đồng ở bước sẵn sàng trả. Khi hủy và bắt đầu xử lý cùng phiên bản, một lệnh trả 204, lệnh kia trả 409 với Problem Details và lịch sử chỉ ghi bước thành công. [Kiểm thử phiếu giặt](../../../tests/LaundryAPI.Tests/OrderTests.cs)
