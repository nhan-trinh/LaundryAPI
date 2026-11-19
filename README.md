# LaundryAPI

## Khởi tạo Manager và đăng nhập bằng Swagger

Cần SDK .NET 10.0.404 và SQL Server. Cấu hình local dùng Windows Authentication với cơ sở dữ liệu `LaundryAPI` trên LocalDB. Tài khoản kết nối cần quyền áp dụng migrations và quyền tạo database nếu database chưa tồn tại.

Chạy PowerShell từ thư mục gốc. Đặt mật khẩu Manager và sinh khóa ký JWT trong .NET User Secrets cho project API.

```powershell
$project = 'src/LaundryAPI/LaundryAPI.csproj'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$credential = Get-Credential -UserName 'manager' -Message 'Nhập mật khẩu khởi tạo Manager'
$key = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($key)
$rng.Dispose()
$OutputEncoding = New-Object System.Text.UTF8Encoding($false)
@{
    'ManagerSeed:Username' = $credential.UserName
    'ManagerSeed:Password' = $credential.GetNetworkCredential().Password
    'Jwt:SigningKey' = [Convert]::ToBase64String($key)
} | ConvertTo-Json | dotnet user-secrets set --project $project
dotnet dev-certs https --trust
dotnet run --project $project --no-launch-profile -- --urls https://localhost:7040
```

Khi khởi động, API áp dụng migrations rồi tạo Manager đầu tiên. Các lần khởi động sau giữ mật khẩu đã lưu. Khi triển khai, cấp `ConnectionStrings__Laundry`, `ManagerSeed__Username`, `ManagerSeed__Password` và `Jwt__SigningKey` qua môi trường hoặc qua nhà cung cấp cấu hình bí mật tương ứng. Khóa ký là chuỗi Base64 của ít nhất 32 byte ngẫu nhiên. Giữ khóa ổn định giữa các lần khởi động.

1. Mở [Swagger UI](https://localhost:7040/swagger/). Chạy `POST /api/v1/auth/login` với `username` và `password`.
2. Sao chép `accessToken` từ phản hồi, chọn **Authorize**, dán giá trị token rồi xác nhận.
3. Chạy `GET /api/v1/auth/me` để xem tên đăng nhập và vai trò `Manager`. Khi đăng xuất khỏi Swagger, gọi lại endpoint này sẽ nhận HTTP 401.

## Kiểm thử

Chạy `dotnet test` từ thư mục gốc. Kiểm thử HTTP dùng một database SQL Server mới mang tên riêng cho mỗi lần chạy và xóa database đó khi kết thúc. Mặc định dùng LocalDB. Để chọn instance khác, cấp chuỗi kết nối qua biến môi trường `LAUNDRY_TEST_SQL` với tài khoản có quyền tạo và xóa database thử nghiệm.
