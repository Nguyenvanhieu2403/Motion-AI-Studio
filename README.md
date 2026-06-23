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

## Cài ComfyUI và Wan 2.1 image-to-video

1. Cài ComfyUI theo hướng dẫn của dự án chính thức, sau đó khởi động với `python main.py --listen 0.0.0.0 --port 8188`.
2. Trong **ComfyUI-Manager**, cài custom node phù hợp workflow Wan 2.1 I2V bạn chọn (và `ComfyUI-VideoHelperSuite` nếu dùng node `VHS_VideoCombine` để xuất H.264 MP4). Tải model Wan 2.1 image-to-video, text encoder và VAE đúng vị trí mà custom node đó yêu cầu.
3. Bật menu developer: mở Settings, chọn **Enable Dev mode options**, refresh trang; sau đó mở workflow Wan 2.1 của bạn trong UI.
4. Thiết kế workflow dọc `576×1024`, `121 frames`, `16 fps` (xấp xỉ 7.6 giây). Dùng ảnh product là image đầu vào chính, ảnh concept là reference/style input nếu node workflow của bạn hỗ trợ; node cuối phải lưu `video/h264-mp4`.
5. Chọn **Save (API Format)** trong menu workflow và dùng file đó để ghi đè [wan21-image-to-video.json](/D:/PersonalProject/Motion%20AI%20Studio/backend/src/VideoGen.Infrastructure/ComfyUI/workflows/wan21-image-to-video.json).

### Template và placeholder

File trong repository là **ComfyUI API Format JSON hợp lệ**, nhưng chỉ là template: các `class_type`/input của Wan khác nhau theo custom node và phiên bản model; không có một graph Wan chính xác cho mọi máy. Sau khi export workflow thật, thay đúng giá trị của nó bằng các placeholder này:

| Phần trong API JSON | Thay bằng |
| --- | --- |
| LoadImage product `inputs.image` | `"{{PRODUCT_IMAGE_PATH}}"` |
| LoadImage/reference `inputs.image` | `"{{REFERENCE_IMAGE_PATH}}"` |
| Text prompt positive/negative | `"{{POSITIVE_PROMPT}}`, `"{{NEGATIVE_PROMPT}}"` |
| Seed | `{{SEED}}` |
| Width / height | `{{WIDTH}}` / `{{HEIGHT}}` |
| Frame count | `{{FRAMES}}` |

String placeholder phải có dấu quote; số **không** có dấu quote. `ComfyUiWorkflowBuilder` chỉ truyền `Path.GetFileName(...)` cho `LoadImage`, JSON-escape prompt an toàn, kiểm tra không còn placeholder và parse/validate graph trước khi `ComfyUiClient` gọi `/prompt`.

### Chia sẻ thư mục ảnh với ComfyUI

`LoadImage` của ComfyUI chỉ tìm file bên trong `ComfyUI/input`. Backend lưu ảnh tại `uploads`, vì vậy hai thư mục cần dùng chung cùng dữ liệu và **chỉ filename** được truyền trong workflow:

- Docker: mount cùng host folder vào `/app/uploads` của backend và `ComfyUI/input` của container ComfyUI.
- Chạy ComfyUI trên Windows host: tạo directory junction từ `ComfyUI/input` tới `backend/data/uploads`, ví dụ PowerShell chạy với quyền phù hợp: `New-Item -ItemType Junction -Path <ComfyUI>\input\motion-studio -Target <workspace>\backend\data\uploads`. Nếu node `LoadImage` không chấp nhận subfolder, trỏ cả `input` trực tiếp vào cùng thư mục hoặc copy/sync filename vào `input`.

Client gửi graph đến `/prompt`, poll `/history/{prompt_id}`, tìm output của node trong cả `history.outputs.*.videos` và `.gifs`, rồi tải file qua `/view` về `outputs/<job-id>.mp4`.

### Test ComfyUI bằng curl / Postman

Trước hết copy hai ảnh vào `ComfyUI/input` và tạo `workflow.test.json` từ API workflow export (thay placeholder bằng filename/prompt cụ thể). Trên PowerShell:

```powershell
$body = @{ prompt = (Get-Content ./workflow.test.json -Raw | ConvertFrom-Json) } | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Post -Uri http://localhost:8188/prompt -ContentType 'application/json' -Body $body
# Lấy prompt_id từ phản hồi, sau đó:
Invoke-RestMethod -Uri http://localhost:8188/history/<prompt_id>
```

Ví dụ curl (Windows dùng `curl.exe`):

```bash
curl.exe -X POST http://localhost:8188/prompt -H "Content-Type: application/json" --data-binary "@workflow.request.json"
curl.exe "http://localhost:8188/history/<prompt_id>"
curl.exe -o result.mp4 "http://localhost:8188/view?filename=<filename>.mp4&type=output"
```

`workflow.request.json` cho curl phải có wrapper `{"prompt": { ...API graph... }}`. Dùng Postman với POST `/prompt`, Body → raw → JSON, rồi GET `/history/{prompt_id}` và GET `/view` với `filename`, `subfolder`, `type` từ output history.

## API

- `POST /api/video-jobs` – multipart: `productImage`, `referenceImage`, `userDescription`, `style`
- `GET /api/video-jobs/{id}` – trạng thái, prompt và URL video khi hoàn thành
- `GET /api/video-jobs/{id}/video` – stream/download MP4

Ảnh chỉ chấp nhận JPG/JPEG/PNG/WEBP, content type ảnh hợp lệ và tối đa 10 MB mỗi file.
