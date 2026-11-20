# 33 — Nghiệm thu v1 để chấm đồ án

Status: ready-for-human
Type: task
Blocked by: 24, 25, 26, 27, 28, 29, 30, 31, 32

Chủ dự án đã chốt mốc v1 sẵn sàng để chấm khi tất cả kiểm thử đều đạt trên SQL Server và người khác có thể tự làm theo README để hoàn thành một lượt giao dịch tại quầy qua Swagger.

## Tiêu chí nghiệm thu

- [x] Chạy `dotnet test` trên SQL Server thử nghiệm theo README và ghi kết quả cho bản nghiệm thu. Cấu hình ứng dụng trong repo vượt qua kiểm tra mật khẩu seed và khóa ký.
- [x] [Kịch bản Swagger trong README](../../../README.md#một-lượt-nhận-và-trả-đồ-tại-quầy) hướng dẫn từ khởi tạo Manager, tạo Staff và hai dịch vụ đến khi Staff đăng nhập, tạo khách và lập phiếu. Với 0,25 kg × 10.002 đồng và 2 chiếc × 30.000 đồng, hai dòng lần lượt là 2.501 đồng và 60.000 đồng, tổng phiếu là 62.501 đồng.
- [ ] Theo README, người chạy dùng Swagger để bắt đầu xử lý, đánh dấu sẵn sàng, thu tiền và trả đồ. Tra lại bằng điện thoại rồi mở phiếu thấy trạng thái Đã trả, đúng một khoản thu 62.501 đồng và lịch sử ghi đúng người bàn giao.

## Comments

Chủ dự án đã chọn nghiệm thu bằng một người chưa quen repo. Người đó tự làm theo README qua Swagger. Nếu phải hỏi thêm, ghi lại bước bị vướng, cập nhật README và để người đó thử lại bước này trước khi đóng ticket.

Ngày 20/11/2026, bản `fc72d9b` và workspace dùng SDK 10.0.404 đều đạt 25/25 test trên SQL Server tại `localhost`. Lượt chạy workspace dùng `dotnet test LaundryAPI.slnx --configuration Release`, mất 1 phút 1 giây và có 0 test bị bỏ qua. Mật khẩu seed dùng cấu hình User Secrets hiện có.

Khi chạy xong, ghi vào Comments mã phiếu demo, số tiền đã thu và username ở sự kiện `Returned`. Nếu bị vướng, ghi bước trong README và kết quả nhìn thấy trên Swagger.
