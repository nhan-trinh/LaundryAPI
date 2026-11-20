# 32 — Tìm phiếu tại quầy

Status: resolved
Type: task
Blocked by: 28

Nhân viên tìm lại phiếu khi khách quay lại và lọc danh sách để xem công việc tại quầy.

## Tiêu chí nghiệm thu

- [x] Tìm bằng mã duy nhất trả đúng phiếu. Tìm bằng số điện thoại trả các phiếu thuộc hồ sơ khách, kể cả khi tìm bằng dạng 0… hoặc +84… tương ứng.
- [x] Lọc theo trạng thái, ngày nhận hoặc kết hợp cả hai. Mọi phiếu trong kết quả thỏa các điều kiện đã chọn.
- [x] Kết quả được phân trang và mặc định xếp theo thời điểm nhận từ mới đến cũ. Trên cùng tập dữ liệu, các trang kế tiếp không lặp hoặc bỏ sót phiếu.
- [x] Từ kết quả, mở chi tiết để đối chiếu mã, khách, các dòng và trạng thái. Phiếu đã hủy vẫn tra cứu được khi dữ liệu có trạng thái này.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. Phạm vi tra cứu của đặc tả v1 được ghi tại quyết định 21. [Biên bản quyết định](../map.md#decisions-so-far)

## Answer

Staff và Manager tra cứu qua `GET /api/v1/orders`. Có thể kết hợp mã phiếu (`code`), số điện thoại (`phoneNumber`), trạng thái hiện tại (`status`) và ngày nhận tại Việt Nam (`receivedDate`, dạng `yyyy-MM-dd`). Khi nhập trực tiếp vào URL, mã hóa dấu `+` thành `%2B`.

Trang đầu là `page=1`. Mặc định mỗi trang có 20 phiếu, có thể đặt `pageSize` từ 1 đến 100. Phiếu nhận gần nhất đứng đầu. Nếu trùng thời điểm nhận, phiếu có ID lớn hơn đứng trước. Dùng mã trong kết quả để mở `GET /api/v1/orders/{code}`, kể cả với phiếu đã hủy. [Kiểm thử HTTP](../../../tests/LaundryAPI.Tests/OrderTests.cs)
