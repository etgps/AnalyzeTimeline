# AnalyzeTimeline

Google Timeline からエクスポートした `Timeline.json` をアップロードし、訪問した**国**および**日本の都道府県**を年別・月別に集計して HTML レポートを生成する Web アプリケーションです。

- 入力した `Timeline.json` からすべての訪問イベントを抽出
- Natural Earth の国境界データと国土数値情報の行政区域データを用いた高精度な点-in-ポリゴン判定
- Google Charts GeoChart による世界・日本地図の可視化
- 年別・月別のフィルタリング対応 HTML レポートをプレビュー・ダウンロード

---

## 動作要件

| ツール | バージョン |
| --- | --- |
| .NET SDK | 10.0 以上 |
| Python | 3.8 以上（地理データ変換スクリプト用） |
| Docker / Docker Compose | 任意（Docker 起動する場合） |

---

## セットアップ

### 1. リポジトリをクローン

```powershell
git clone https://github.com/etgps/AnalyzeTimeline.git
cd AnalyzeTimeline
```

### 2. 国境界データの準備（Natural Earth）

国分類に使用する GeoJSON ファイルを生成します。

#### 2-1. GeoPackage をダウンロード

[Natural Earth Vector](https://www.naturalearthdata.com/downloads/) から `natural_earth_vector.gpkg` をダウンロードし、以下のパスに配置します。

```
data/packages/natural_earth_vector.gpkg
```

> 参考: [Natural Earth GitHub](https://github.com/nvkelso/natural-earth-vector/releases) の `natural_earth_vector.gpkg.zip` を解凍して配置します。

#### 2-2. GeoJSON を生成

```powershell
python scripts/extract_natural_earth_countries.py
```

実行後、`data/natural_earth_countries.geojson` が生成されます。

---

### 3. 都道府県データの準備（国土数値情報）

日本の都道府県分類に使用する GeoJSON を配置します。

#### 3-1. 行政区域データをダウンロード

[国土数値情報ダウンロードサービス](https://nlftp.mlit.go.jp/ksj/gml/datalist/KsjTmplt-N03-v3_1.html) から「行政区域データ（N03）」をダウンロードします。

#### 3-2. GeoJSON を配置

ダウンロードした ZIP を解凍し、以下のパスに `.geojson` ファイルを配置します。

```
data/N03-20260101_GML/N03-20260101_prefecture.geojson
```

> ファイル名は取得時期によって異なります（例: `N03-20240101_prefecture.geojson`）。アプリはパスを自動検索するため、フォルダ構成を合わせれば日付部分が異なっても動作します。

---

## ローカル起動

```powershell
dotnet run --project src/Api/AnalyzeTimeline.Api.csproj
```

起動後、`http://localhost:53043` をブラウザで開きます（ポートは起動時ログで確認できます）。

> **初回起動時**: GeoJSON の読み込みとインデックス構築に数十秒かかります。2回目以降はキャッシュされるため高速です。

---

## Docker 起動

```powershell
docker compose -f docker/docker-compose.yml up --build
```

起動後、`http://localhost:8080` で利用できます。

> Docker 起動時は、`data/` フォルダをコンテナにマウントするか、Dockerfile 内でコピーするよう設定が必要です。

---

## 使い方

1. ブラウザで起動 URL を開く
2. 「Timeline.json を選択」から Google Timeline の JSON ファイルを選択  
   （`Google Takeout > 位置情報の履歴 > Timeline.json` からエクスポートできます）
3. 「分析開始」をクリック
4. 生成されたレポートをプレビュー確認後、「HTML をダウンロード」でローカル保存

---

## プロジェクト構成

```text
/src
  /Api             ASP.NET Core Web API（エンドポイント、静的ファイル配信）
  /Application     ビジネスロジック（パース、地域分類、レポート生成）
  /Domain          ドメインモデル（集計結果、訪問地域）
  /Infrastructure  インフラ層（現在はプレースホルダー）
/tests             テストプロジェクト（未整備）
/docker            Dockerfile / docker-compose.yml
/scripts           データ変換スクリプト
/data              地理データ（Git 管理外、手動配置が必要）
/sampledata        検証用サンプルデータ（Git 管理外）
/docs              補足ドキュメント・サンプル JSON
```

---

## データファイルについて

以下のファイルはサイズが大きいため Git 管理外です。上記セットアップ手順に従って手動で配置してください。

| ファイル | サイズ目安 | 用途 |
| --- | --- | --- |
| `data/packages/natural_earth_vector.gpkg` | ~450 MB | 国境界データ（変換元） |
| `data/natural_earth_countries.geojson` | ~14 MB | 国分類用（スクリプト生成） |
| `data/N03-20260101_GML/N03-20260101_prefecture.geojson` | ~520 MB | 都道府県分類用 |
| `sampledata/Timeline.json` | ~100 MB 以上 | 動作検証用サンプル |

> データファイルが存在しない場合、アプリは簡易境界ボックス判定にフォールバックします（精度が低下します）。

---

## 性能実績

`sampledata/Timeline.json`（約 118 MB）での検証結果：

| 項目 | 結果 |
| --- | --- |
| 抽出地点数 | 23,636 件 |
| 分類済み地点数 | 46,646 件 |
| 年別訪問先数 | 58 件 |
| 月別訪問先数 | 262 件 |
| 初回処理時間（GeoJSON 読込込み） | 約 78 秒 |
| 2回目以降（キャッシュ利用） | 約 10 秒 |
