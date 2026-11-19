# 24 — Khởi tạo Manager và đăng nhập qua Swagger

Status: resolved
Type: task
Blocked by: —

Trên SQL Server mới, người thiết lập có thể khởi tạo Manager đầu tiên rồi đăng nhập bằng Swagger để thử API nội bộ.

## Tiêu chí nghiệm thu

- [x] Khởi chạy một project C# ASP.NET Core Web API với SDK .NET 10.0.404. Áp dụng được EF Core migrations lên SQL Server trống và mở được Swagger UI cho `/api/v1`.
- [x] Mật khẩu khởi tạo lấy từ .NET User Secrets ở local, từ biến môi trường hoặc secret manager khi triển khai. Cấu hình được commit không chứa mật khẩu mặc định.
- [x] Seed chỉ tạo Manager khi tài khoản chưa tồn tại. Khởi động lại với giá trị seed khác cho cùng tài khoản vẫn cho đăng nhập bằng mật khẩu hiện có và từ chối giá trị mới.
- [x] Qua Swagger, đăng nhập đúng nhận JWT và dùng được JWT để gọi một thao tác cần xác thực. Sai mật khẩu hoặc gọi thao tác được bảo vệ khi chưa đăng nhập bị từ chối.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Comments

Đã triển khai migration `InitialAccounts`, seed Manager và hai thao tác `POST /api/v1/auth/login`, `GET /api/v1/auth/me`. [Hướng dẫn chạy Swagger và kiểm thử](../../../README.md).

Build bằng SDK 10.0.404 và kiểm thử cấu hình bí mật đã qua. EF Core sinh được script SQL và xác nhận model khớp migration. Kiểm thử HTTP qua hai lần khởi động đang chờ SQL Server thử nghiệm. Trong phiên triển khai, LocalDB không tạo được instance và kết nối Windows Authentication đến SQL Server tại localhost gặp lỗi SSPI.

## Answer

Ngày 19/11/2026, cả hai kiểm thử đã qua trên SQL Server tại `localhost` với SDK 10.0.404. Kiểm thử tích hợp khởi động API trên database mới, tải Swagger UI và tài liệu OpenAPI, đăng nhập lấy JWT rồi dùng token đọc `/api/v1/auth/me`. Sai mật khẩu và gọi endpoint thiếu token đều trả HTTP 401. Sau khi khởi động lại với mật khẩu seed khác, mật khẩu ban đầu vẫn đăng nhập được và giá trị seed mới bị từ chối.
