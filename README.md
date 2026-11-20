# LaundryAPI

## Khởi tạo Manager và đăng nhập bằng Swagger

Cần SDK .NET 10.0.404 và SQL Server. Cấu hình local dùng Windows Authentication với cơ sở dữ liệu `LaundryAPI` trên LocalDB. Tài khoản kết nối cần quyền áp dụng migrations và quyền tạo database nếu database chưa tồn tại.

Cho lần demo đầu tiên, mở PowerShell từ thư mục gốc. Đoạn lệnh sau chọn database riêng `LaundryDemo`, đặt mật khẩu Manager và sinh khóa ký JWT trong .NET User Secrets. Chọn server phù hợp với máy ở dòng `ConnectionStrings__Laundry`: giữ `(localdb)\MSSQLLocalDB` nếu dùng LocalDB, hoặc đổi thành `localhost` nếu dùng instance SQL Server mặc định.

```powershell
$project = 'src/LaundryAPI/LaundryAPI.csproj'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Laundry = 'Server=(localdb)\MSSQLLocalDB;Database=LaundryDemo;Integrated Security=true;TrustServerCertificate=true'
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

Nếu đã cấu hình User Secrets trên máy, dùng lại thông tin đã thiết lập. Chỉ cần đặt các biến môi trường cho môi trường Development và database demo như trên, rồi chạy lại dòng `dotnet run` với `--project src/LaundryAPI/LaundryAPI.csproj`.

1. Mở [Swagger UI](https://localhost:7040/swagger/). Chạy `POST /api/v1/auth/login` với `username` và `password`.
2. Sao chép `accessToken` từ phản hồi, chọn **Authorize**, dán giá trị token rồi xác nhận.
3. Chạy `GET /api/v1/auth/me` để xem tên đăng nhập và vai trò `Manager`. Khi đăng xuất khỏi Swagger, gọi lại endpoint này sẽ nhận HTTP 401.

## Một lượt nhận và trả đồ tại quầy

Giữ cửa sổ chạy API mở. Trong Swagger, mở từng endpoint, chọn **Try it out**, nhập dữ liệu rồi chọn **Execute**. Đọc mã HTTP và **Response body** trong phần **Server response**. Các bước dưới đây dùng tiền mặt.

**1. Manager chuẩn bị tài khoản và bảng giá.** Sau khi đăng nhập bằng Manager, gọi `POST /api/v1/staff-accounts` với `username` là `linh-demo` và `password` do bạn tự đặt. Khi nhận 201, giữ mật khẩu để đăng nhập ở bước 2. Nếu tên đã tồn tại và bạn chưa biết mật khẩu, chọn một tên mới rồi dùng tên đó trong các bước sau.

Gọi `POST /api/v1/services` hai lần. Mỗi lần nhận 201, ghi lại `id` ứng với dịch vụ vừa tạo.

| Dịch vụ | JSON gửi đi |
| --- | --- |
| Giặt sấy theo kg | `{"name":"Giặt sấy","unit":"kg","unitPriceVnd":10002}` |
| Giặt chăn theo chiếc | `{"name":"Giặt chăn","unit":"piece","unitPriceVnd":30000}` |

**2. Staff nhận đồ.** Gọi `POST /api/v1/auth/login` bằng tài khoản Staff vừa tạo. Trong **Authorize**, chọn **Logout** để bỏ token Manager, dán `accessToken` mới rồi chọn **Authorize**. Gọi `GET /api/v1/auth/me` và kiểm tra `role` là `Staff`.

Gọi `POST /api/v1/customers` với `{"name":"Khách demo","phoneNumber":"0912345678"}`. Ghi lại `id` trong phản hồi 201 (hoặc 200 nếu số đã có hồ sơ). Sau đó gọi `POST /api/v1/orders` với JSON dưới đây. Thay `customerId` và cả hai `serviceId` bằng các ID thực vừa ghi, đúng thứ tự kg rồi chiếc.

```json
{
  "customerId": 1,
  "lines": [
    { "serviceId": 1, "quantity": 0.25 },
    { "serviceId": 2, "quantity": 2 }
  ]
}
```

Phản hồi 201 có `status` là `Received`. Kiểm tra `amountVnd` trong hai dòng lần lượt là `2501` và `60000`, còn `totalVnd` là `62501`. Sao chép `code` và `version` của phiếu.

**3. Staff xử lý và bàn giao.** Nhập cùng mã phiếu vào ô `code` ở mỗi endpoint bên dưới. Trong JSON, thay chữ `VERSION` bằng chuỗi `version` vừa đọc, giữ nguyên dấu ngoặc kép. Mỗi lệnh thành công trả 204 với phần thân trống. Sau **mỗi** lệnh, gọi `GET /api/v1/orders/{code}` để xem trạng thái và lấy `version` mới cho lệnh kế tiếp.

| Lệnh trong Swagger | JSON gửi đi | Trạng thái khi đọc lại |
| --- | --- | --- |
| `POST /api/v1/orders/{code}/start-processing` | `{"version":"VERSION"}` | `Processing` |
| `POST /api/v1/orders/{code}/mark-ready` | `{"version":"VERSION"}` | `Ready` |
| `POST /api/v1/orders/{code}/handover` | `{"version":"VERSION","paymentMethod":"cash"}` | `Returned` |

**4. Tra lại khi khách quay về quầy.** Gọi `GET /api/v1/orders`, nhập `0912345678` vào ô `PhoneNumber`. Tìm đúng `code` vừa lập rồi mở `GET /api/v1/orders/{code}` theo mã đó. Đối chiếu ba kết quả:

- Phiếu ở trạng thái `Returned`, với một khoản thu trong `payment` có `method` là `cash` và `amountVnd` là `62501`.
- `history` ghi đủ bốn trạng thái theo thứ tự `Received`, `Processing`, `Ready`, `Returned`.
- Sự kiện `Returned` ghi `username` của Staff đã bàn giao và thời điểm `occurredAt`.

## Kiểm thử

Mở cửa sổ PowerShell thứ hai ở thư mục gốc và chạy `dotnet test`. Bộ test mặc định dùng LocalDB, tự tạo các database `LaundryTest_…` rồi xóa chúng khi kết thúc. Tài khoản Windows chạy test cần quyền tạo và xóa database thử nghiệm.

Nếu máy dùng instance SQL Server mặc định ở `localhost`, chạy trong cửa sổ kiểm thử:

```powershell
$env:LAUNDRY_TEST_SQL = 'Server=localhost;Integrated Security=true;TrustServerCertificate=true'
dotnet test
```
