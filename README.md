# Alt Tap-Hold (C#)

Windows 向けの常駐アプリです。左 Alt の単独タップを無変換、右 Alt の単独タップを変換にします。150 ms 未満の単独タップだけを変換し、長押しまたは他キーとの組み合わせでは元の左右 Alt として動かします。

短い単独タップでは物理 Alt down/up を抑止し、無変換／変換の down/up だけを注入します。Esc やメニュー解除用キーは送信しません。

## 必要環境

- Windows 10/11 と .NET 8 SDK（ビルド時）
- 日本語キーボード配列。AltGr を使う配列は対象外です。
- 通常権限のデスクトップアプリ。管理者として動くアプリを操作するには、このアプリも同等の権限が必要になる場合があります。

実装は WinForms の通知領域アイコンと低レベルキーボードフックを使います。x86 と x64 の発行をサポートします。Windows 10/11 は .NET 8 のサポート対象ですが、旧 32-bit Windows をサポートするものではありません。

## ビルドと状態テスト

PowerShell で次を実行します。

```powershell
dotnet build .\AltTapHold.sln -c Release
dotnet run --project .\AltTapHold.Tests -c Release
```

テストは実キーを送信せず、時計・修飾状態・送信結果を差し替えて、タップ境界、Alt+Tab の順序、先押し修飾、左右の重なり、5 秒期限、送信失敗後の解放を確認します。

## 配布用 exe

```powershell
dotnet publish .\AltTapHold\AltTapHold.csproj -c Release -r win-x64 --self-contained false -o "$env:USERPROFILE\Desktop\AltTapHold-win-x64"
dotnet publish .\AltTapHold\AltTapHold.csproj -c Release -r win-x86 --self-contained false -o "$env:USERPROFILE\Desktop\AltTapHold-win-x86"
```

それぞれの出力フォルダー名でアーキテクチャを区別します。`\\wsl.localhost\...` のような WSL 共有内の exe は Windows から直接実行しません。必ず上記のような Windows ローカルドライブ上の出力を起動してください。exe 起動後は通知領域の `Alt Tap-Hold` を右クリックし、`終了` で停止します。二重起動は同一ユーザーセッション内の名前付き mutex で防ぎます。

これはフレームワーク依存型の配布です。実行する Windows に .NET 8 Desktop Runtime（SDK を入れている場合はすでに含まれます）が必要ですが、配布物は非常に小さくなり、ランタイムのセキュリティ更新も OS 側の .NET 更新で受け取れます。ランタイム未導入の PC へ単体で渡す場合だけ、`--self-contained true -p:PublishSingleFile=true` を指定した自己完結型を使います。

## 調整と安全策

[`AltStateMachine.cs`](AltTapHold/AltStateMachine.cs) の `TapThresholdMs`（現在 150）と `MaximumHoldMs`（現在 5000）が調整箇所です。5 秒に達した記録は破棄され、Active の Alt だけは up を送ります。送信が失敗した場合は Alt up だけを小さな解放待ちとして再試行し、成功するまで物理入力を抑止しません。

ロック／解除通知や、Alt 以外のキーの押下履歴は保持しません。強制終了、OS 停止、UIPI による注入拒否では押しっぱなしの完全な回復は保証できません。

## アイコン

EXE と通知領域には、[`Assets/AltTapHold.ico`](Assets/AltTapHold.ico) を使います。透明背景、白い外縁の濃紺キーキャップ、左の青緑と右の橙の矢印で構成し、ライト／ダーク モードの双方で視認できるようにしています。元のベクター図は [`Assets/AltTapHold.svg`](Assets/AltTapHold.svg) で、`powershell -ExecutionPolicy Bypass -File .\tools\GenerateIcons.ps1` により ICO を再生成できます。

## 検証記録

2026-09-22 時点で、ソースと状態テストを作成しました。この作業環境には .NET SDK と Windows の実キーボード環境がないため、ビルド、x86/x64 発行、IME、Alt+Tab、配布 exe 起動、ロック復帰の実機確認は未実施です。上の手順を Windows 上で実行し、未実施項目を確認してください。
