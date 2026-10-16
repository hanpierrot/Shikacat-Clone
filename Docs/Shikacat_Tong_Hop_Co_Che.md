# Shikacat Clone: Tổng hợp cơ chế đã làm và phần cần bổ sung

> Mỗi tiêu đề `##` tương ứng một slide. Các mục `###` là tiểu mục trong slide hoặc slide phụ.
> Cập nhật theo trạng thái project đến ngày 16/10/2026.

---

## 1. Tổng quan dự án

- **Thể loại:** puzzle 2D kiểu Shikaku (mobile, màn hình dọc).
- **Luật chơi:** kéo chọn các hình chữ nhật (phòng) trên lưới. Mỗi phòng chứa đúng một ô clue, giá trị clue là diện tích phòng. Thắng khi lưới được phủ kín bởi các phòng hợp lệ.
- **Công nghệ:** Unity 2022.3, C# 9, Built-in Render Pipeline, TextMeshPro, UGUI, DOTween, Spine, UIParticle.
- **Dữ liệu level:** JSON (`levels.json`, hiện có **100 level**) cùng file cấu hình sinh level `generator_config.json`.
- **Kiểm thử:** bộ EditMode test (luật chơi, solver, generator) với NUnit.

---

## 2. Gameplay cốt lõi (đã xong)

| Cơ chế | Mô tả |
|---|---|
| Lưới theo level | Dựng từ dữ liệu, kích thước ô tự co theo màn hình, có khoảng cách giữa các ô |
| Kéo để dựng phòng | Preview xanh (hợp lệ) hoặc đỏ (không hợp lệ), phân biệt nhấp và kéo |
| Thay phòng | Kéo đè lên phòng cũ sẽ thay thế, tính 1 lượt |
| Huỷ phòng | Bấm vào phòng để huỷ |
| Điều kiện thắng | Lưới đầy và mọi phòng có diện tích đúng với clue |
| Giới hạn | Time, Moves hoặc cả hai (0 nghĩa là tắt) |
| Bộ đếm | Chỉ chạy sau khi người chơi bấm cell đầu tiên |
| Khoá input | Khoá khi game kết thúc và khoá các ô của phòng đang chạy animation |
| Chi tiết | Khoá khi bấm ra ngoài lưới hoặc UI, chống bấm đôi khi đang đánh giá preview |

---

## 3. Các loại clue và ô đặc biệt (đã xong)

### Clue
| Loại | Quy tắc |
|---|---|
| Normal | Số = diện tích phòng |
| Locked | Có bộ đếm số phòng cần dựng để mở khoá. Khoá lại nếu huỷ phòng làm tụt số lượng |
| Horizontal (H) | Phòng phải có chiều rộng ≥ chiều cao |
| Vertical (V) | Phòng phải có chiều rộng ≤ chiều cao |
| Square (S) | Phòng phải vuông |
| Hidden | Hiện `?`, đặt phòng kích thước bất kỳ, kiểm tra diện tích thật khi lưới đầy |
| **Kép** | **Locked hoặc Hidden kèm một hình dạng H/V/S** |

### Ô tắt (hole)
- Ô không chơi, không cho phép phòng phủ lên, không tính vào diện tích.
- Hole được sinh theo cụm phòng liền kề (không rời rạc).

---

## 4. Hệ thống phòng và mèo (đã xong)

- **Màu ngẫu nhiên khi dựng:** dữ liệu level không lưu màu. Mỗi phòng chọn màu ngẫu nhiên, tránh trùng với phòng kề cạnh. Huỷ rồi dựng lại có thể ra màu khác.
- **10 màu mèo** theo skin Spine: Blue, Red, Green, Pink, Purple, Teal, Neon, Orange, Yellow, White.
- **Shader panel:** một GameObject gộp sàn và tường, đồng thời ghi stencil để mèo bị cắt trong lòng phòng.
- **Mèo Spine:** phóng to từ 0, nhảy (`jump*`), hạ xuống, rồi `front_idle*`. Trong lúc intro tạm bỏ cắt stencil và nâng Order in Layer.
- **Order in Layer:** phòng và mèo tự nâng lên trong lúc animation để không bị phòng lân cận che.

---

## 5. Animation và VFX (đã xong)

| Hiệu ứng | Mô tả |
|---|---|
| Dựng phòng (DOTween) | Phóng to từ tâm, lơ lửng một nhịp, hạ cánh, nảy nhẹ |
| Mèo xuất hiện | Phóng to, nhảy, hạ xuống, idle |
| Text diện tích | Hiện phía trên vị trí chạm khi kéo, cách một khoảng cố định theo % chiều cao màn hình |
| Mở khoá clue | Đầu khoá bay lên, tắt ổ khoá, hiện số với hiệu ứng nảy |
| Combo | Particle theo mốc: Like, Nice, Great, Wow, Beauty, Perfect, Amazing (chạy ngay khi nhả tay, đúng ô cuối) |
| Hiệu ứng thắng | ShotFX sau khi phòng và mèo cuối xong, rồi DropFX khi mở panel thắng |

### Hệ thống combo
- Mỗi phòng đặt thành công: combo +1.
- Huỷ phòng hoặc đặt sai (preview đỏ): về 0.
- Đặt đè phòng cũ: huỷ (về 0), tạo (thành 1).
- Particle dùng pool, chạy trong Canvas bằng `UIParticle`.

---

## 6. HUD và UI (đã xong phần logic)

- **HUD UGUI:** thanh tiến trình (time hoặc moves), 3 sao đè lên thanh (mất sao khi limit tụt dưới vị trí sao), text level, text limit kèm icon (time hoặc moves).
- **Quy tắc hiển thị:** level có cả time và moves thì thanh = time, text = moves.
- **Game Over:** panel thắng và thua, nút Next, Restart. Luồng thắng: chờ animation → ShotFX → panel + DropFX.
- **Responsive:** nền chính phủ kín màn hình, lưới tự co theo vùng hiển thị. Hướng dẫn thiết lập Canvas Scaler và Aspect Ratio Fitter đã có.

---

## 7. Hệ thống level (đã xong)

### Dữ liệu và luật
- Định dạng JSON: kích thước lưới, clue, hole, giới hạn, lời giải (solution).
- Luật tách riêng thành module thuần (`RoomRules`) dùng chung cho game, solver và tool.
- Validate đầy đủ: tổng giá trị clue, lời giải hợp lệ, hình dạng khớp, giới hạn khoá.

### Solver
- Backtracking theo ứng viên phòng của từng clue, giới hạn số nút.
- Kiểm tra: có lời giải không, duy nhất không, lịch mở khoá clue Locked khả thi không.

---

## 8. Level Manager (Editor tool, đã xong)

- Đọc, thêm, nhân bản, xoá, đổi thứ tự level; lưu kèm bản sao lưu tự động (`LevelBackups/`).
- **Brush tạo phòng:** kéo hình chữ nhật, rồi chọn một ô bất kỳ trong phòng để đặt clue (loại, hình dạng, số lượt mở khoá). Giá trị tự = diện tích.
- Brush Hole, Erase, Select; xem lời giải bằng viền phòng.
- Solver ngay trong tool: kiểm tra duy nhất, ghi lời giải đầu tiên.
- Nạp level vào scene (Edit/Play) hoặc chơi từ level đang chọn.
- Panel Generate: sinh theo khoảng level, ghi vào file đặt tên, có báo cáo batch theo tier.

---

## 9. Trình sinh level tự động (đã xong)

### Quy trình
1. Chia lưới thành phòng theo phân bố hình dạng rút ra từ 205 level tham chiếu.
2. Đặt clue (ưu tiên góc phòng).
3. Biến một số cụm phòng liền kề thành hole.
4. Gán loại clue theo lịch mở khoá.
5. Kiểm tra bằng solver, sửa trùng lời giải bằng cách thêm ràng buộc hoặc dời clue.
6. Gán giới hạn time, moves hoặc cả hai theo lịch.

### Cấu hình theo level
| Nội dung | Thiết lập hiện tại |
|---|---|
| Kích thước lưới | Đường cong tăng dần: 3×3 ở level 1, 5 đến 6 ở level 10, 7 đến 8 ở level 20, đến 11 đến 12 ở level 100 |
| Mốc mechanic | Hole từ level 21, Hidden từ 26, Shaped từ 31, Locked từ 36, **clue kép từ 41** |
| Level giới thiệu | Mỗi mechanic mới bắt buộc xuất hiện đúng ở level mở khoá |
| Tier | Easy (1 đến 20), Medium (21 đến 50), Hard (51 trở đi) |
| Giới hạn | Khối level chỉ time, khối chỉ moves, level chẵn chục từ 30 dùng cả hai |

### Kết quả batch gần nhất
- Easy, Medium, Hard: 100% level đạt lời giải duy nhất, thời gian sinh vài ms mỗi level.

---

## 10. Kiến trúc và kỹ thuật

- **Singleton** chỉ dùng cho `GridManager`, `GameManager` (dịch vụ toàn cục). Còn lại giao tiếp qua sự kiện (`RoomCommitted`, `RoomRemoved`, `CellPressed`, `PlacementFailed`, `RoomAnimationFinished`, `LevelLoaded`).
- **Object pooling:** ô lưới, panel phòng, effect combo.
- **Assembly definition:** `Shikacat.Runtime`, `Shikacat.Editor`, `Tests`.
- **Dữ liệu tách khỏi logic:** màu lấy từ `CatPalette`, ngưỡng sinh level lấy từ JSON.
- **Hiệu ứng tách khỏi luật:** logic thắng, thua không phụ thuộc animation, chỉ chờ tín hiệu hoàn tất.

---

## 11. Phần cần bổ sung: ưu tiên cao

| Hạng mục | Mô tả | Ghi chú |
|---|---|---|
| **Âm thanh** | `AudioManager`, nhạc nền, SFX (kéo, dựng, huỷ, sai, mở khoá, thắng, thua, bấm nút, popup) | Đã có sẵn các file `.ogg` trong `Assets/Audio`, chưa nối vào game |
| **Lưu tiến trình** | Lưu level đã qua, số sao đạt được | Cần cho mọi tính năng meta |
| **Chọn level / chapter** | Màn hình chọn level, khoá và mở khoá theo tiến trình | Phụ thuộc lưu tiến trình |
| **Tutorial** | Hướng dẫn luật chơi và giới thiệu mechanic tại các level 21, 26, 31, 36, 41 | Điểm kích hoạt trùng lịch của generator |
| **Màn kết thúc game** | Khi chơi hết level cuối | Hiện chỉ có Restart khi không còn level kế |
| **Pause / Settings** | Âm lượng, rung, ngôn ngữ | |

---

## 12. Phần cần bổ sung: ưu tiên trung bình

| Hạng mục | Mô tả |
|---|---|
| **Booster** | Hint (dùng lời giải đã lưu), Time Freeze hoặc +Time, Undo phòng gần nhất, Extra Moves, khung booster chung |
| **Hiệu ứng thua** | Animation và VFX khi thua, tương ứng hiệu ứng thắng |
| **Cảnh báo sắp hết giờ** | Nhấp nháy đồng hồ, đổi màu thanh tiến trình |
| **Art UI hoàn chỉnh** | Nút Game Over, popup Pause, màn chọn level (chờ bộ asset và giao diện mẫu) |
| **Safe Area và Canvas Scaler** | Áp dụng đồng bộ cho mọi UI, kiểm thử nhiều tỉ lệ màn |
| **Cân bằng level** | Chơi thử 100 level, chỉnh `moveSlack`, `timePerRoom`, mật độ mechanic theo từng tier |

---

## 13. Phần cần bổ sung: tuỳ chọn và hoàn thiện

- Rung (haptic) trên mobile.
- Đa ngôn ngữ (Việt, Anh).
- Tool: Undo trong Level Manager, bản xem trước level, sinh clue kép thủ công và theo tier.
- Generator: tinh chỉnh mật độ clue kép, kiểm tra độ khó (số lời giải, độ sâu tìm kiếm) để làm đường cong khó.
- Analytics cơ bản (tỉ lệ thắng, thua theo level).

---

## 14. Việc dọn dẹp và rủi ro cần xử lý

| Vấn đề | Hành động |
|---|---|
| Cảnh báo `TextureImporter.spritesheet` trong package Spine | Bỏ qua hoặc cập nhật package |
| Phụ thuộc `UIParticle` (package `com.coffee.ui-particle`) | Ghi rõ trong tài liệu setup, bảo đảm package có trong `manifest.json` |
| Instance effect phải nằm trong Canvas | Kiểm tra scene trước khi build |
| Scene cũ còn override `m_Enabled` trên Cell | Làm mới Cell trong scene hoặc revert override |
| Chưa chạy trên thiết bị thật | Cần test hiệu năng (particle, Spine, stencil) trên máy cấu hình thấp |
| Dữ liệu level cũ (`color`) | Vẫn đọc được, tự gọn đi sau lần Save kế tiếp |

---

## 15. Lộ trình đề xuất

1. **Giai đoạn A: âm thanh và lưu tiến trình.** Nối SFX, BGM sẵn có; lưu tiến độ.
2. **Giai đoạn B: meta.** Chọn level, màn kết thúc, Pause và Settings.
3. **Giai đoạn C: tutorial và booster.** Tutorial theo level mở khoá; booster Hint, Undo trước.
4. **Giai đoạn D: art UI và đánh bóng.** Thay UI tạm bằng bộ asset cuối, hiệu ứng thua, cảnh báo giờ.
5. **Giai đoạn E: cân bằng và QA.** Chơi thử level, tinh chỉnh generator, test thiết bị thật.
6. **Giai đoạn F: phát hành.** Icon, splash, thiết lập build, ảnh store.

---

## 16. Tóm tắt trạng thái

| Mảng | Trạng thái |
|---|---|
| Gameplay cốt lõi và luật | Hoàn thành |
| Mechanic clue (Locked, shaped, Hidden, kép) và hole | Hoàn thành |
| Level tool, solver, generator | Hoàn thành, đã có 100 level |
| Animation, VFX, mèo Spine, combo, hiệu ứng thắng | Hoàn thành |
| HUD UGUI | Hoàn thành phần logic, chờ art cuối |
| Âm thanh | Có asset, chưa nối |
| Meta (lưu, chọn level, tutorial, settings) | Chưa làm |
| Booster | Chưa làm |
| Art UI cuối cùng, QA, phát hành | Chưa làm |
