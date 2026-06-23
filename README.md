# Motion AI Studio

MVP tạo video giới thiệu sản phẩm dọc (TikTok/Reels): Angular 18 uploads hai ảnh, .NET 8 tạo và theo dõi job, GPT Vision viết prompt, ComfyUI/Wan 2.1 render MP4.

## Kiến trúc

`Angular → POST /api/video-jobs → PostgreSQL → background worker → OpenAI Vision → ComfyUI /prompt → MP4`

Video và ảnh chỉ được phục vụ qua endpoint API; đường dẫn vật lý không đi ra client.

## Chạy bằng Docker

1. Sao chép `.env.example` thành `.env`, rồi đặt `OPENAI_API_KEY` và `COMFYUI_BASE_URL`.
2. Chạy `docker compose up --build`.
3. Mở `http://localhost:4200`. Swagger API ở `http://localhost:8080/swagger`.

ComfyUI không nằm trong compose. Nếu chạy trên máy host, dùng `COMFYUI_BASE_URL=http://host.docker.internal:8188`; nếu backend cũng chạy trực tiếp, dùng `http://localhost:8188`.

## Chạy local (không Docker)

Yêu cầu .NET 8 SDK, Node 20+, PostgreSQL 16 và ComfyUI đang chạy. Tạo database theo connection string trong `backend/src/VideoGen.Api/appsettings.json`, đưa API key vào User Secrets hoặc biến môi trường `OpenAI__ApiKey`, sau đó:

```powershell
dotnet run --project backend/src/VideoGen.Api
cd frontend
npm install
npm start
```

API tự gọi `Database.Migrate()` khi khởi động. Migration gốc nằm tại `backend/src/VideoGen.Infrastructure/Persistence/Migrations`.

## Chuẩn bị workflow Wan 2.1

`backend/src/VideoGen.Infrastructure/ComfyUI/workflows/wan21-image-to-video.json` là mẫu cấu trúc, **không thể render ngay** vì mỗi bản cài ComfyUI có node ID/class khác nhau. Trong ComfyUI:

1. Cài Wan 2.1 image-to-video và tạo workflow 9:16, 5–10 giây, có node xuất MP4 (`SaveVideo`, VHS hoặc node tương đương).
2. Chọn **Save (API Format)**, ghi đè file mẫu nêu trên.
3. Giữ các placeholder không có dấu quote cho số: `{{SEED}}`, `{{WIDTH}}`, `{{HEIGHT}}`, `{{FRAMES}}`; giữ trong quote cho chuỗi: `{{PRODUCT_IMAGE_PATH}}`, `{{REFERENCE_IMAGE_PATH}}`, `{{POSITIVE_PROMPT}}`, `{{NEGATIVE_PROMPT}}`.
4. Upload/copy hai ảnh từ thư mục `uploads` của API vào ComfyUI `input` với đúng filename. Với ComfyUI chạy ngoài Docker, hãy map/sync thư mục này vào `ComfyUI/input` (symlink hoặc shared volume).

Client gửi workflow tới `/prompt`, poll `/history/{prompt_id}` và tải MP4 qua `/view`. Template cần để node lưu video xuất dữ liệu xuất hiện trong `history.outputs` (mảng `gifs` hoặc `videos`).

## API

- `POST /api/video-jobs` – multipart: `productImage`, `referenceImage`, `userDescription`, `style`
- `GET /api/video-jobs/{id}` – trạng thái, prompt và URL video khi hoàn thành
- `GET /api/video-jobs/{id}/video` – stream/download MP4

Ảnh chỉ chấp nhận JPG/JPEG/PNG/WEBP, content type ảnh hợp lệ và tối đa 10 MB mỗi file.
