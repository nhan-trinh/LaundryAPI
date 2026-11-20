# Đặc tả LaundryAPI v1

Status: ready-for-agent

**Chủ dự án đã duyệt đặc tả v1 và cách chia ticket 24–32.** Phạm vi được phép ở bước này là ghi ticket vào tracker.

## Problem Statement

Tại một cửa hàng giặt là, khách có thể giao cả quần áo tính theo kg và chăn tính theo chiếc trong cùng một lần. Nhân viên cần biết đã nhận những gì, đã báo giá bao nhiêu, toàn bộ đồ đã sẵn sàng chưa và ai đã thu tiền khi trả đồ. Nếu hai nhân viên cùng mở một phiếu, việc một người hủy trong lúc người kia bắt đầu xử lý có thể khiến cửa hàng mất căn cứ bàn giao.

## Solution

Xây dựng REST API nội bộ cho Staff và Manager. Mỗi lần nhận đồ tạo một **phiếu giặt** gắn với một khách hàng. Từng **dòng dịch vụ** ghi lượng đồ và giá đã báo. Cả phiếu có một tiến độ chung. Khi toàn bộ đồ sẵn sàng, nhân viên xác nhận thu đủ tiền và trả đồ trong một thao tác. Swagger UI cho phép thử trọn quy trình của đồ án từ đăng nhập đến tra lại phiếu.

## User Stories

1. Là nhân viên, tôi muốn đăng nhập bằng tài khoản riêng để mỗi thao tác trên phiếu được ghi đúng người.
2. Là quản lý, tôi muốn cấp hoặc khóa tài khoản nhân viên để kiểm soát ai còn được thao tác tại quầy.
3. Là người thiết lập hệ thống, tôi muốn cấp mật khẩu Manager đầu tiên qua cấu hình bí mật để khởi tạo cửa hàng an toàn và giữ mật khẩu hiện có qua các lần khởi động.
4. Là nhân viên, tôi muốn tìm khách bằng số điện thoại hoặc tạo khách bằng tên và số điện thoại để tiếp nhận đồ ngay tại quầy.
5. Là quản lý, tôi muốn quản lý tên dịch vụ, đơn vị tính, đơn giá và việc ngừng cung cấp để nhân viên chọn đúng dịch vụ đang bán.
6. Là nhân viên, tôi muốn ghi nhiều dòng theo kg hoặc theo chiếc trên cùng phiếu để khách chỉ cần một mã nhận đồ.
7. Là nhân viên, tôi muốn ghi đặc điểm của đồ ở từng dòng và hẹn giờ trả cho cả phiếu khi cần để đồng nghiệp có thông tin bàn giao.
8. Là nhân viên, tôi muốn lưu giá tại lúc thêm dòng và thấy tổng tiền tính từ các dòng để giải thích số tiền phải thu dù bảng giá sau đó thay đổi.
9. Là nhân viên, tôi muốn sửa dòng dịch vụ trong lúc phiếu còn ở quầy để sửa sai sót trước khi bắt đầu xử lý.
10. Là nhân viên, tôi muốn hủy một phiếu vừa nhận và ghi lý do để cửa hàng tra lại được cả phiếu lẫn đồ đã ghi nhận.
11. Là nhân viên, tôi muốn đánh dấu bắt đầu xử lý và chỉ đánh dấu sẵn sàng khi toàn bộ đồ đã xong để khách nhận đủ một lần.
12. Là quản lý, tôi muốn các dòng trên phiếu được khóa ngay khi bắt đầu xử lý để giữ căn cứ tính tiền và bàn giao.
13. Là nhân viên, tôi muốn chọn tiền mặt hoặc chuyển khoản lúc bàn giao để ghi nhận đúng một lần thu đủ tiền và trả toàn bộ đồ.
14. Là nhân viên, tôi muốn biết khi đồng nghiệp vừa cập nhật cùng phiếu để tải lại trước khi thao tác tiếp.
15. Là nhân viên, tôi muốn tìm phiếu theo mã hoặc điện thoại và lọc danh sách theo ngày nhận, trạng thái để tra cứu ngay khi khách quay lại.
16. Là quản lý, tôi muốn đọc lịch sử trạng thái của phiếu, kể cả phiếu đã hủy hoặc do người đã nghỉ việc xử lý, để đối chiếu ai làm gì và khi nào.
17. Là người thử API của đồ án, tôi muốn đăng nhập trên Swagger và nhận lỗi có cấu trúc để kiểm chứng cả luồng thành công lẫn thao tác bị từ chối.

## Implementation Decisions

### Kiến trúc và truy cập · Quyết định 01–05, 11–12, 17, 22–23

- Một cửa hàng, một project C# ASP.NET Core Web API, SDK .NET 10.0.404. Kiến trúc client–server, phát triển API-first. Dùng EF Core với SQL Server theo hướng code-first và quản lý lược đồ bằng migrations.
- Controllers nhận yêu cầu HTTP qua DTOs. Services thực thi quy tắc nghiệp vụ. Repositories truy cập dữ liệu qua EF Core. Tổ chức Models, Data, Validations, Middlewares và Exceptions thành các layer theo thư mục trong cùng project.
- REST API dùng tiền tố `/api/v1`. Swagger UI hỗ trợ đăng nhập bằng tên đăng nhập và mật khẩu, lấy JWT rồi dùng token để thử các thao tác cần xác thực.
- Staff quản lý khách hàng và xử lý phiếu. Manager có toàn bộ quyền Staff và quản lý thêm danh mục dịch vụ, bảng giá, tài khoản nhân viên. Khóa tài khoản có hiệu lực ngay từ yêu cầu API tiếp theo, kể cả với JWT còn hạn. Lịch sử vẫn liên kết đến người đã thao tác.
- Mật khẩu khởi tạo Manager lấy từ .NET User Secrets ở local, từ biến môi trường hoặc secret manager khi triển khai. Không ghi mật khẩu mặc định vào appsettings.json hay commit mật khẩu vào Git. Seed chỉ tạo tài khoản khi chưa tồn tại. Chạy lại seed hoặc khởi động lại phải giữ mật khẩu hiện có.

### Tiếp nhận, tính giá và tra cứu · Quyết định 04, 06, 08, 15, 18–21

- Nhận và trả đồ tại quầy. Chuẩn hóa số điện thoại Việt Nam khi tạo hồ sơ và tìm khách. Hai dạng tương ứng `0…` và `+84…` cùng nhận diện một hồ sơ. Người nhà dùng chung số sẽ dùng chung hồ sơ.
- Mỗi phiếu có mã duy nhất, dễ đọc, thuộc một khách và có ít nhất một dòng dịch vụ. Ghi chú trên từng dòng và thời điểm hẹn trả trên phiếu đều tùy chọn. Tra phiếu bằng mã hoặc điện thoại, lọc theo trạng thái và ngày nhận, phân trang và mặc định xếp từ mới đến cũ theo thời điểm nhận.
- Mỗi dịch vụ có một đơn vị tính và một đơn giá VND. Nhân viên cân hoặc đếm tại quầy. Kg là số dương có tối đa hai chữ số thập phân. Chiếc là số nguyên dương. Thành tiền từng dòng bằng số lượng nhân đơn giá, làm tròn đến đồng gần nhất, nửa đồng làm tròn lên. Tổng phiếu bằng tổng các thành tiền đã làm tròn.
- Khi thêm dòng, lưu tên dịch vụ, đơn vị tính và đơn giá đang áp dụng. Các giá trị đã lưu được giữ trên phiếu kể cả khi danh mục đổi tên, đổi giá hoặc ngừng dịch vụ. Chỉ dịch vụ đang cung cấp mới được chọn cho dòng mới.

### Vòng đời, thanh toán và lỗi API · Quyết định 07, 09–10, 13–14, 16, 23

| Trạng thái hiện tại | Thao tác hợp lệ | Kết quả |
|---|---|---|
| Vừa lập phiếu | Ghi nhận đồ đã tiếp nhận | Phiếu ở Đã nhận (`Received`). |
| Đã nhận | Sửa dòng dịch vụ | Tính lại tiền theo các dòng trên phiếu. Phiếu vẫn ở Đã nhận. |
| Đã nhận | Bắt đầu xử lý | Phiếu sang Đang xử lý (`Processing`). Khóa các dòng dịch vụ cho cả Staff và Manager. |
| Đã nhận | Hủy với lý do có nội dung | Phiếu sang Đã hủy (`Cancelled`). Giữ toàn bộ phiếu và các dòng để tra cứu. |
| Đang xử lý | Đánh dấu toàn bộ đồ đã sẵn sàng | Phiếu sang Sẵn sàng trả. Nếu mới hoàn tất một phần, phiếu vẫn ở Đang xử lý. |
| Sẵn sàng trả | Thu đủ bằng tiền mặt hoặc chuyển khoản và bàn giao toàn bộ | Ghi nhận một lần thanh toán và chuyển sang Đã trả trong cùng giao dịch. |

- Đã trả và Đã hủy là trạng thái cuối. Khi hủy, lưu `CancellationReason`, `CancelledAt`, `CancelledBy`. Xác định người hủy từ tài khoản đăng nhập. V1 không cho khôi phục phiếu đã hủy.
- Chi tiết phiếu hiển thị lịch sử từ khi lập. Mỗi lần lập hoặc chuyển trạng thái đều ghi trạng thái mới, thời điểm và tài khoản thực hiện.
- Dùng endpoint riêng cho bắt đầu xử lý, đánh dấu sẵn sàng, trả đồ kèm thu tiền và hủy. Với các yêu cầu ghi cạnh tranh dựa trên cùng một phiên bản phiếu, chỉ thao tác đầu tiên thành công. Client nhận HTTP 409 khi dùng phiên bản cũ và cần tải lại phiếu.
- Lỗi có cấu trúc Problem Details. Dữ liệu không hợp lệ, ví dụ thiếu lý do hủy, trả HTTP 400. Xung đột với trạng thái hoặc phiên bản phiếu trả HTTP 409. Nếu một phần của thao tác thu tiền và trả đồ thất bại, toàn bộ giao dịch được hoàn tác.

## Testing Decisions

**Điểm kiểm thử đã duyệt** là giao diện HTTP công khai của `/api/v1`, chạy với SQL Server thử nghiệm đã áp dụng migrations. Kiểm tra phản hồi và đọc lại tài nguyên qua API để xác nhận kết quả. Các kỳ vọng này áp dụng cho mọi cách tổ chức lớp bên trong project. Repo hiện có tài liệu nghiệp vụ, chưa có bộ kiểm thử làm mẫu. Với cấu hình bí mật và seed, bổ sung kiểm tra cấu hình và hành vi qua lần khởi động kế tiếp.

| Phạm vi | Tình huống và kết quả cần quan sát |
|---|---|
| Xác thực và phân quyền | Đăng nhập lấy JWT. Staff có thể lập phiếu nhưng bị từ chối khi quản lý bảng giá. Manager khóa Staff, rồi dùng chính JWT còn hạn của Staff để gọi lại API. Yêu cầu bị từ chối và lịch sử phiếu vẫn hiển thị người đã thao tác. |
| Khởi tạo Manager | Dùng cấu hình bí mật để khởi tạo trên DB mới. Khởi động lại với một giá trị seed khác cho cùng tài khoản. Thông tin đăng nhập đã có vẫn dùng được, giá trị seed mới không thay thế mật khẩu. Kiểm tra cấu hình trong repo không chứa mật khẩu khởi tạo. |
| Khách hàng và tra cứu | Tạo khách bằng số dạng nội địa rồi tìm bằng dạng +84 tương ứng. Cả hai dẫn tới cùng hồ sơ. Tìm phiếu bằng mã và bằng điện thoại, kiểm tra bộ lọc ngày, trạng thái, thứ tự và phân trang. |
| Lập phiếu và tính tiền | Lập phiếu cho một khách với các dòng tính theo kg và chiếc. Kiểm tra ví dụ 0,25 kg × 10.002 đồng = 2.500,5 đồng, làm tròn thành 2.501 đồng. Thêm 2 chiếc × 30.000 đồng thì tổng là 62.501 đồng. Từ chối phiếu thiếu khách, phiếu rỗng, lượng bằng 0, kg có ba chữ số thập phân hoặc số chiếc lẻ. |
| Giá tại thời điểm nhận | Lập phiếu rồi đổi tên, giá và ngừng dịch vụ. Đọc lại phiếu thấy tên, đơn vị và giá ban đầu. Thử thêm dịch vụ đã ngừng vào dòng mới phải bị từ chối. |
| Khóa và hủy phiếu | Sửa lượng khi Đã nhận và kiểm tra tổng tiền. Hủy thiếu lý do trả 400. Hủy hợp lệ giữ đủ các dòng, người hủy, thời điểm và lý do. Từ chối khôi phục. Trên một phiếu đã sang Đang xử lý, sửa lượng hoặc hủy đều trả 409 và giữ nội dung cũ. |
| Tranh chấp giữa hai nhân viên | Từ cùng phiên bản Đã nhận, gửi lệnh bắt đầu xử lý và lệnh hủy. Chỉ một lệnh thành công, lệnh còn lại nhận 409. Đọc lại phiếu và lịch sử để đối chiếu với lệnh thành công. |
| Thanh toán và bàn giao | Kiểm tra trọn luồng cho cả tiền mặt và chuyển khoản. Mỗi phiếu Đã trả có đúng một lần thu đủ. Hai yêu cầu bàn giao dựa trên cùng phiên bản cũng chỉ ghi một lần. Yêu cầu bàn giao thất bại phải giữ nguyên cả tình trạng thanh toán và trạng thái phiếu. |
| Hợp đồng API | Thử các thao tác nghiệp vụ qua Swagger UI dưới phiên bản v1. Kiểm tra nội dung Problem Details cùng mã 400 và 409 cho các tình huống tương ứng. |

## Out of Scope

- Frontend, tài khoản khách tự phục vụ, giao nhận tận nơi và vận hành nhiều chi nhánh.
- Theo dõi tiến độ, thanh toán hoặc bàn giao riêng từng dòng. V1 thu đủ và trả toàn bộ đồ một lần.
- Khôi phục phiếu đã hủy, ghi ngoại lệ hoặc phê duyệt điều chỉnh sau khi bắt đầu xử lý. Nếu đã ghi 5 kg và phát hiện khối lượng thực là 3 kg sau khi chuyển sang Đang xử lý, v1 vẫn tính tiền theo 5 kg đã ghi.

## Further Notes

Danh sách quyết định 01–23 là căn cứ đối chiếu khi duyệt phạm vi. ADR 0001 ghi lý do khóa các dòng dịch vụ từ khi bắt đầu xử lý. [Biên bản quyết định](map.md) · [ADR 0001](../../docs/adr/0001-khoa-phieu-giat.md)
