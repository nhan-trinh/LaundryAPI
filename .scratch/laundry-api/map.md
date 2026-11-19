# Thiết kế LaundryAPI

## Notes

[Bản đặc tả](spec.md) ghi các ràng buộc do chủ dự án đưa ra.

## Decisions-so-far

Các yêu cầu kỹ thuật ban đầu nằm trong bản đặc tả.

- [01 — Cửa hàng và chi nhánh](issues/01-cua-hang.md) — Một cửa hàng tại một địa điểm cho đồ án kết thúc môn.
- [02 — Người dùng và kênh tiếp nhận](issues/02-nguoi-dung.md) — Nhân viên và quản lý dùng hệ thống nội bộ. Kênh nhận trả đồ được xác nhận ở câu 06.
- [03 — Phạm vi vận hành](issues/03-pham-vi.md) — Quản lý khách hàng, dịch vụ và bảng giá, lập phiếu giặt, theo dõi xử lý, ghi nhận thanh toán và trả đồ.
- [04 — Đơn vị tính giá](issues/04-tinh-gia.md) — Một phiếu giặt có thể gồm nhiều dòng dịch vụ, kết hợp dòng tính theo kg và dòng tính theo chiếc.
- [05 — Cách tổ chức solution và ORM](issues/05-solution.md) — Một project Web API, chia layer bằng thư mục. Lựa chọn ORM cụ thể được xác nhận ở câu 12.

- [06 — Tiếp nhận khách](issues/06-tiep-nhan.md) — Nhận và trả đồ tại quầy. Tra khách theo số điện thoại, tạo hồ sơ bằng tên và số điện thoại.
- [07 — Tiến độ và trả đồ](issues/07-trang-thai.md) — Theo dõi cả phiếu qua các bước Đã nhận → Đang xử lý → Sẵn sàng trả → Đã trả. Bàn giao toàn bộ đồ một lần.
- [08 — Chốt giá](issues/08-gia.md) — Cân hoặc đếm tại quầy và lưu đơn giá VND của dịch vụ vào từng dòng khi lập phiếu. Thay đổi bảng giá chỉ áp dụng cho dòng mới.
- [09 — Thanh toán](issues/09-thanh-toan.md) — Khi bàn giao, một thao tác ghi nhận thu đủ một lần bằng tiền mặt hoặc chuyển khoản và đánh dấu đã trả đồ.
- [10 — Sửa và hủy phiếu](issues/10-sua-huy.md) — Chỉ sửa dòng hoặc hủy khi `Received`. Khi hủy phải có lý do, lưu `CancelledAt`, `CancelledBy`, `CancellationReason` và giữ nguyên `Order` cùng `Order Items`. `Cancelled` là trạng thái cuối trong v1. Từ `Processing`, khóa dòng dịch vụ. Mọi điều chỉnh về sau cần quy trình được cấp quyền hoặc cơ chế ghi ngoại lệ có nhật ký.
- [11 — Phân quyền](issues/11-phan-quyen.md) — Staff thao tác với khách hàng và phiếu giặt. Manager có toàn bộ quyền của Staff, thêm quyền quản lý dịch vụ, bảng giá và tài khoản nhân viên.
- [12 — ORM và migrations](issues/12-orm.md) — Dùng EF Core với SQL Server theo hướng code-first, quản lý lược đồ bằng migrations.

- [13 — Sai sót sau khi khóa phiếu](issues/13-ngoai-le.md) — V1 từ chối sửa cho cả Staff và Manager. Để chức năng ghi nhận ngoại lệ hoặc phê duyệt điều chỉnh sang phiên bản sau.
- [14 — Lịch sử trạng thái](issues/14-nhat-ky.md) — Lưu trạng thái, thời điểm và tài khoản thực hiện mỗi lần lập hoặc chuyển trạng thái. Hiển thị lịch sử trong chi tiết phiếu.
- [15 — Số lượng và làm tròn](issues/15-so-luong.md) — Kg là số dương, tối đa hai chữ số thập phân. Chiếc là số nguyên dương. Làm tròn từng dòng đến đồng gần nhất, nửa đồng làm tròn lên rồi cộng thành tổng phiếu.
- [16 — Hai người cùng thao tác](issues/16-dong-thoi.md) — Chỉ ghi nhận thao tác đầu tiên thành công. Yêu cầu dùng phiên bản cũ nhận HTTP 409 để tải lại phiếu. Thanh toán và bàn giao được lưu trong cùng giao dịch.
- [17 — Đăng nhập nội bộ](issues/17-dang-nhap.md) — Mỗi người có tài khoản riêng, đăng nhập bằng tên đăng nhập và mật khẩu để lấy JWT dùng được trong Swagger. Quản lý cấp hoặc khóa tài khoản nhân viên.

- [18 — Nhận diện khách hàng](issues/18-so-dien-thoai.md) — Chuẩn hóa số Việt Nam trước khi tra cứu và yêu cầu mỗi số chỉ thuộc một hồ sơ. Người nhà dùng chung số sẽ dùng chung hồ sơ trong v1.
- [19 — Dịch vụ ngừng kinh doanh](issues/19-danh-muc.md) — Lưu tên, đơn vị và đơn giá tại thời điểm thêm dòng. Ngừng cung cấp bằng cách ẩn dịch vụ khỏi danh sách chọn cho dòng mới.
- [20 — Thông tin trên phiếu](issues/20-phieu-tai-quay.md) — Mỗi phiếu cần một khách và ít nhất một dòng. Cho phép ghi chú từng dòng và nhập thời điểm hẹn trả trên phiếu, cả hai đều tùy chọn.
- [21 — Tìm phiếu tại quầy](issues/21-tra-cuu.md) — Cấp mã phiếu duy nhất dễ đọc. Cho tìm bằng mã hoặc số điện thoại, lọc theo trạng thái và ngày nhận, phân trang và mặc định xếp phiếu mới nhất trước.
- [22 — Tài khoản quản lý ban đầu](issues/22-quan-ly-dau-tien.md) — Khởi tạo Manager đầu tiên từ thông tin cấu hình riêng khi thiết lập hệ thống. Khóa tài khoản nhân viên có hiệu lực từ yêu cầu API tiếp theo, kể cả với token còn hạn, và giữ liên kết lịch sử. Mật khẩu khởi tạo Manager được cấp qua .NET User Secrets ở local và qua biến môi trường hoặc secret manager khi triển khai. Không ghi mật khẩu mặc định vào `appsettings.json` hoặc commit mật khẩu vào Git. Seed chỉ tạo tài khoản khi chưa tồn tại. Chạy lại hoặc khởi động lại ứng dụng không ghi đè mật khẩu hiện có.
- [23 — Hợp đồng API v1](issues/23-hop-dong-api.md) — Dùng endpoint riêng để bắt đầu xử lý, đánh dấu sẵn sàng, trả đồ kèm thanh toán và hủy. Trả lỗi theo Problem Details, dùng HTTP 400 cho dữ liệu sai và 409 khi thao tác xung đột với trạng thái hoặc phiên bản phiếu.

- [24 — Khởi tạo Manager và đăng nhập qua Swagger](issues/24-khoi-tao-manager.md) — Đã nghiệm thu trên SQL Server. Manager đăng nhập lấy JWT và giữ mật khẩu hiện có qua lần khởi động lại.
- [25 — Cấp và khóa tài khoản nhân viên](issues/25-tai-khoan-nhan-vien.md) — Manager cấp và khóa tài khoản Staff. Khóa giữ nguyên danh tính và thu hồi quyền truy cập từ yêu cầu kế tiếp, kể cả với JWT còn hạn.

## Fog

Đã trả lời các câu hỏi 01–23. Chủ dự án đã duyệt đặc tả v1 và cách chia ticket triển khai.

## Ticket triển khai

- [24 — Khởi tạo Manager và đăng nhập qua Swagger](issues/24-khoi-tao-manager.md)
- [25 — Cấp và khóa tài khoản nhân viên](issues/25-tai-khoan-nhan-vien.md)
- [26 — Tạo và tìm khách theo số điện thoại](issues/26-ho-so-khach-hang.md)
- [27 — Quản lý dịch vụ và bảng giá](issues/27-dich-vu-va-bang-gia.md)
- [28 — Lập và đọc lại phiếu giặt](issues/28-lap-phieu-giat.md)
- [29 — Sửa hoặc hủy phiếu khi vừa nhận](issues/29-sua-huy-phieu.md)
- [30 — Bắt đầu xử lý và đánh dấu sẵn sàng trả](issues/30-xu-ly-phieu.md)
- [31 — Thu tiền và trả toàn bộ đồ](issues/31-thu-tien-tra-do.md)
- [32 — Tìm phiếu tại quầy](issues/32-tim-phieu-tai-quay.md)
