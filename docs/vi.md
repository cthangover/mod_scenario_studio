# Scenario Studio — gỡ lỗi và chỉnh sửa kịch bản khi trò chơi đang chạy

Scenario Studio là một bảng gắn bên cạnh trò chơi đang chạy, cho biết kịch bản nào đang chơi và dòng nào đang hiện trên màn hình. Nó có thể tạm dừng hội thoại, đi từng dòng một, dừng ở điểm ngắt, dịch dòng tiếp theo, khởi động lại kịch bản với trạng thái game được hoàn lại, và sửa kịch bản ngay tại chỗ — rồi lập tức hiện thay đổi trong game.

Nó có hai chế độ: **Người mới** hiển thị khoảnh khắc câu chuyện bằng các khối đơn giản; **Tác giả** hiển thị văn bản kịch bản với số dòng.

## Mở nó

Trò chơi có bộ Developer Tools (cơ chế *Scenario Studio*) sẽ có nút **Xưởng** — cây bút lông — trên thanh công cụ. Cờ khởi động `--studio` mở bảng ngay khi bắt đầu. Các hình dưới đây chụp từ căn bếp của Cthangover; các chỉnh sửa được ghi vào một lớp thử nghiệm, không đụng tới tệp của game.

## Chế độ Người mới

![Căn bếp với bảng Xưởng bên phải ở chế độ Người mới: cảnh home_kitchen, kịch bản home_kitchen_cooking, huy hiệu lớp; dưới "Đang trên màn hình" là dòng "Dòng mới" và câu hỏi "Một căn phòng được biến thành bếp"; các câu trả lời Rời đi (chuyển tới cảnh home_vestibule), Nấu ăn (hành động toggle_cooking_workbench) và một câu trả lời mới, mỗi cái có "Chọn nơi đến" và Sửa; các nút "+ Dòng trước câu hỏi" và "+ Câu trả lời"; hộp "Thay đổi này ảnh hưởng đến" với lớp đích, và Áp dụng ở dưới.](assets/studio-novice.jpg)

Thẻ **Hiện tại** hiển thị khoảnh khắc trên màn hình: dòng, câu hỏi và từng câu trả lời cùng nơi nó dẫn tới. **Sửa** đổi một đoạn văn bản, **Chọn nơi đến** đổi nơi một câu trả lời dẫn tới (một nhãn, cảnh khác, hay kết thúc), **+ Dòng** và **+ Câu trả lời** thêm vào khoảnh khắc. Mỗi thay đổi đều cho biết trong **Thay đổi này ảnh hưởng đến** rằng nó sẽ được ghi vào mod hay lớp nào.

## Chế độ Tác giả, tạm dừng và điểm ngắt

![Bảng ở chế độ Tác giả: Tạm dừng, Từng bước, Tiếp tục, Tài nguyên và Khởi động lại trên thanh điều khiển; văn bản kịch bản của home_kitchen với dòng 6 — câu select "Một căn phòng được biến thành bếp" — được đánh dấu chấm đỏ điểm ngắt và ký hiệu ▶ của dòng hiện tại; bên dưới là "sau dòng 9" với + Dòng, + Lựa chọn, + Đến cảnh, và lớp đích my_studio.](assets/dock-breakpoint.jpg)

- **Tạm dừng** giữ hội thoại trước dòng tiếp theo; **Từng bước** chạy một dòng; **Tiếp tục** chơi tiếp.
- Bấm vào lề là đặt một **điểm ngắt**: hội thoại dừng trước dòng đó.
- Kéo ký hiệu **▶** sang dòng khác sẽ khiến dòng đó chạy tiếp theo.
- **+ Dòng**, **+ Lựa chọn**, **+ Đến cảnh** thêm vào sau dòng hiện tại; **Sửa văn bản** sửa cả kịch bản.

## Áp dụng và khởi động lại

**Áp dụng** ghi các thay đổi và chạy lại kịch bản. Ở đây một dòng "Dòng mới" được thêm trước câu hỏi; sau khi áp dụng nó đã hiện trên màn hình:

![Bảng sau khi áp dụng: trên màn hình là "Dòng mới", trạng thái "Đã áp dụng cho "my_studio", kịch bản được khởi động lại và hoàn trạng thái".](assets/dock-applied.jpg)

**Khởi động lại** đưa ra các lựa chọn: chạy lại kịch bản không hoàn trạng thái, chạy lại **có hoàn trạng thái** (cờ, vật phẩm, đồng hồ và mọi thứ trở lại như khi kịch bản bắt đầu), vào lại cảnh, hay tiếp tục từ nhãn hiện tại.

## Xem trạng thái

![Thẻ Trạng thái: "Có thể đổi các giá trị để thử nhánh khác", đồng hồ chỉ ngày 24 giờ 23:00.](assets/dock-state.jpg)

Thẻ **Trạng thái** hiển thị trạng thái game theo từng chủ sở hữu: đồng hồ, các biến của hội thoại đang chạy, các nhiệm vụ mà kịch bản nhắc tới, túi đồ và những kịch bản đã chạy. Biến, số lượng vật phẩm và trạng thái nhiệm vụ có thể đổi (Enter ghi chúng vào game) để thử nhánh khác. **Cảnh** liệt kê các kịch bản của cảnh hiện tại, **Bản đồ** cho thấy các cảnh và đường đi giữa chúng (và nơi thả phông nền mới), còn **Nhật ký** ghi lại những gì đã xảy ra:

![Thẻ Nhật ký: vào home_kitchen, home_kitchen_cooking bắt đầu, chỉnh sửa "+6 → my_studio", các lần khởi động lại, "điểm ngắt, dòng 6", "dòng tiếp theo chuyển tới 5", "điểm kiểm tra trong home_kitchen".](assets/dock-log.jpg)

## Thay đổi được ghi vào đâu

- Mod cài dưới dạng thư mục có thể được sửa trực tiếp.
- Nếu không, thay đổi sẽ vào **lớp xưởng** của game (`user://studio/<game>`), lớp này thắng mọi mod và không đụng tới các mod. Lớp có thể được xuất thành mod mới, rồi thêm vào bộ sưu tập của game.
- **Tài nguyên** đọc lại các phông nền thả vào mod hoặc vào thư mục `backgrounds/` của lớp trong khi game chạy; sau đó có thể tạo cảnh mới trên phông nền đó ngay từ thẻ Bản đồ.

## Khi có trục trặc

1. **Không có nút Xưởng.** Bộ sưu tập của game không chứa xưởng; hãy thêm bộ Developer Tools hoặc chạy với `--studio`.
2. **Từng bước không làm gì.** Hội thoại đang chờ một câu trả lời: hãy chọn một cái trước.
3. **Người chơi khác không thấy thay đổi.** Thay đổi trong lớp xưởng chỉ ở lại game này trên máy này; hãy xuất lớp thành mod để chia sẻ.
