# AnalyzeTimeline

Google Timeline からエクスポートした `Timeline.json` をアップロードし、年別・月別の訪問先レポート HTML を生成する Web application です。

## ローカル起動

```powershell
dotnet run --project src/Api/AnalyzeTimeline.Api.csproj
```

起動後、表示された URL をブラウザで開きます。

## Docker 起動

```powershell
docker compose -f docker/docker-compose.yml up --build
```

`http://localhost:8080` で利用できます。

## 仕様メモ

- 入力画面で `Timeline.json` を選択し、分析を開始します。
- 出力画面では生成された HTML をプレビューし、ダウンロードできます。
- 地域分類はオフラインで動作するよう、国と都道府県の簡易境界ボックスで判定しています。
