# 26 — Tạo và tìm khách theo số điện thoại

Status: resolved
Type: task
Blocked by: 24

Staff hoặc Manager tiếp nhận khách bằng tên và số điện thoại, rồi tìm lại cùng hồ sơ ở lần giao đồ sau.

## Tiêu chí nghiệm thu

- [x] Tạo hồ sơ bằng tên và số điện thoại. Yêu cầu thiếu tên hoặc số điện thoại trả HTTP 400 với Problem Details.
- [x] Tạo bằng dạng nội địa rồi tìm bằng dạng +84 tương ứng, hoặc ngược lại, đều trả về cùng một khách.
- [x] Tạo lại bằng cách viết tương đương không sinh hồ sơ thứ hai, kể cả khi hai yêu cầu tạo đến cùng lúc. Người nhà dùng chung số dùng chung hồ sơ.

Kiểm chứng qua HTTP dưới `/api/v1` với SQL Server thử nghiệm đã áp dụng migrations. Có thể thử thao tác trong Swagger UI. [Đặc tả v1](../spec.md)

## Answer

Staff và Manager tạo khách bằng `POST /api/v1/customers` với `name` và `phoneNumber`. Hồ sơ mới trả HTTP 201. Nếu số đã có, API trả HTTP 200 cùng mã và tên đã lưu. Tra cứu bằng `GET /api/v1/customers?phoneNumber=0912345678` hoặc `GET /api/v1/customers?phoneNumber=%2B84912345678`.

Ba kiểm thử HTTP đã qua trên SQL Server sau khi áp dụng migration `CustomerProfiles`, bao gồm hai yêu cầu tạo đồng thời cho cùng số. [Kiểm thử hồ sơ khách](../../../tests/LaundryAPI.Tests/CustomerTests.cs)
