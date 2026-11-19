# 25 — Cấp và khóa tài khoản nhân viên

Status: resolved
Type: task
Blocked by: 24

Manager cấp tài khoản riêng cho Staff và thu hồi quyền truy cập khi nhân viên nghỉ việc.

## Tiêu chí nghiệm thu

- [x] Manager tạo tài khoản Staff. Nhân viên đăng nhập bằng tài khoản vừa cấp và sử dụng JWT riêng.
- [x] Staff bị từ chối khi gọi API cấp hoặc khóa tài khoản.
- [x] Sau khi Manager khóa tài khoản, ngay yêu cầu API kế tiếp dùng JWT còn hạn của Staff cũng bị từ chối. Lần đăng nhập mới của tài khoản bị khóa bị từ chối.
- [x] Việc khóa giữ lại danh tính nhân viên để các ghi nhận lịch sử vẫn liên kết với người đã thao tác.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Answer

Manager gọi `POST /api/v1/staff-accounts` với `username` và `password` để cấp tài khoản Staff và nhận ID (201). Gọi `POST /api/v1/staff-accounts/{id}/lock` để khóa (204). SQL Server giữ bản ghi, ID và tên đăng nhập của người đã thao tác. Hai yêu cầu cấp cùng tên chỉ tạo được một tài khoản, kể cả khi gửi đồng thời.

Kiểm thử HTTP trên SQL Server xác nhận Staff đăng nhập được bằng tài khoản vừa cấp. Staff gọi hai thao tác quản lý đều nhận 403. Sau khi Manager khóa, cùng JWT gọi lại `GET /api/v1/auth/me` nhận 401, đăng nhập mới cũng nhận 401. Manager vẫn khóa lại được đúng ID và yêu cầu cấp lại tên đã khóa nhận 409.
