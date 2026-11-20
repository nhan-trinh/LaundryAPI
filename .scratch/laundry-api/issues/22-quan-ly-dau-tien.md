# Tài khoản quản lý ban đầu

Status: resolved
Type: grilling
Blocked by: 11, 17

Cấp tài khoản Manager đầu tiên và bảo toàn nhật ký khi nhân viên nghỉ việc như thế nào?

## Comments

Đề xuất. Khởi tạo Manager đầu tiên từ thông tin cấu hình riêng khi thiết lập hệ thống. Khóa tài khoản nhân viên có hiệu lực từ yêu cầu API tiếp theo, kể cả với token còn hạn, và giữ liên kết lịch sử.

## Answer

Khởi tạo Manager đầu tiên từ thông tin cấu hình riêng khi thiết lập hệ thống. Khóa tài khoản nhân viên có hiệu lực từ yêu cầu API tiếp theo, kể cả với token còn hạn, và giữ liên kết lịch sử. Mật khẩu khởi tạo Manager được cấp qua .NET User Secrets ở local và qua biến môi trường hoặc secret manager khi triển khai. Không ghi mật khẩu mặc định vào `appsettings.json` hoặc commit mật khẩu vào Git. Seed chỉ tạo tài khoản khi chưa tồn tại. Chạy lại hoặc khởi động lại ứng dụng không ghi đè mật khẩu hiện có.
