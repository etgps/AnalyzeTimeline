# AnalyzeTimeline 仕様

## 本アプリについて

AnalyzeTimeline は、Google Timeline からエクスポートされた `Timeline.json` をもとに、訪問した国および日本の都道府県を分析する Web application です。

ユーザーが `Timeline.json` をアップロードすると、訪問先を年別・月別に集計し、生成された HTML レポートを画面内でプレビューできます。生成された HTML はダウンロードできます。

## 基本構成

- Web application として提供します。
- バックエンドは ASP.NET Core Web API で実装します。
- フロントエンドは `wwwroot` 配下の静的 HTML / CSS / JavaScript で実装します。
- Docker container として起動できる構成にします。
- 展開先は `etgps.net/analyzetimeline` を想定します。
- ローカル開発では `http://localhost:5088` で起動します。

## ディレクトリ構成

```text
/src
  /Api
  /Application
  /Domain
  /Infrastructure
  /Frontend
/tests
  /ApiTests
  /ApplicationTests
  /DomainTests
/docker
/docs
/data
```

## 入力ファイル

### 対象ファイル

- Google Timeline export の `Timeline.json` を読み込みます。
- 例: `G:\My Drive\99_temp\Timeline (1).json`
- 検証用データとして `sampledata\Timeline.json` を利用します。

### ファイルサイズ

- 115MB 程度の `Timeline.json` を処理できる必要があります。
- アップロード上限は 512MB とします。
- 512MB を超えるファイルはフロントエンド側で警告し、API 側でも拒否します。

### 対応する Timeline JSON 形式

主に Google Timeline の `semanticSegments` 形式に対応します。

訪問先の抽出では、移動経路ではなく訪問候補を優先します。

優先して利用する座標:

```json
{
  "semanticSegments": [
    {
      "startTime": "2026-01-01T12:00:00.000+09:00",
      "visit": {
        "topCandidate": {
          "placeLocation": {
            "latLng": "33.5858121°, 130.419654°"
          }
        }
      }
    }
  ]
}
```

対応する座標形式:

- `placeLocation.latLng`: `"緯度°, 経度°"`
- `placeLocation`: `"geo:緯度,経度"`
- `point`: `"緯度°, 経度°"`
- `latitudeE7` / `longitudeE7`
- `latitude` / `longitude`
- `lat` / `lng`

ただし、`semanticSegments[].visit.topCandidate.placeLocation` が存在する場合は、それを訪問先として扱います。`timelinePath` の移動経路点は、訪問していない地域の誤検出を避けるため、通常の訪問先集計には使用しません。

## 分析仕様

### 抽出対象

- `semanticSegments[].visit` を訪問イベントとして扱います。
- 訪問日時は `semanticSegments[].startTime` を優先します。
- 同一座標・同一分単位の重複データは集約します。

### 国分類

- 国分類は Natural Earth の国境界データを利用して高精度に判定します。
- ソースデータは `data\natural_earth_vector.gpkg.zip` に配置された GeoPackage とします。
- `ne_10m_admin_0_countries` レイヤーからアプリ用の `data\natural_earth_countries.geojson` を生成して利用します。
- GeoJSON の `properties.code` を国コード、`properties.name` を国名として利用します。
- `Polygon` / `MultiPolygon` の座標を点-in-ポリゴン判定します。
- Natural Earth の生成済み GeoJSON が見つからない場合のみ、簡易境界ボックス判定に fallback します。
- 初回読み込み後はポリゴンデータをメモリにキャッシュします。
- 0.1度グリッドの空間インデックスを利用し、判定対象ポリゴンを絞り込みます。
- 同一座標の分類結果はリクエスト内でキャッシュします。

### 日本の都道府県分類

日本国内の都道府県分類は、国土数値情報の行政区域データを利用して高精度に判定します。

利用データ:

```text
data\N03-20260101_GML\N03-20260101_prefecture.geojson
```

仕様:

- GeoJSON の `FeatureCollection` を読み込みます。
- `properties.N03_001` を都道府県名として利用します。
- `Polygon` / `MultiPolygon` の座標を点-in-ポリゴン判定します。
- 行政区域データが見つからない場合のみ、簡易境界ボックス判定に fallback します。
- 初回読み込み後はポリゴンデータをメモリにキャッシュします。
- 0.1度グリッドの空間インデックスを利用し、判定対象ポリゴンを絞り込みます。
- 同一座標の分類結果はリクエスト内でキャッシュします。

### 誤検出対策

これまで確認された誤検出を防ぐ必要があります。

- 移動経路点による未訪問国の検出を避けること。
- 福岡市周辺を South Korea と誤判定しないこと。
- 西日本を China と誤判定しないこと。
- 未訪問の長野県を表示しないこと。
- 訪問済みの福岡県を表示すること。
- 国の訪問先判定は Natural Earth の国境界ポリゴンに基づいて行うこと。

## 集計仕様

### 年別訪問先

- 国および都道府県ごとに集計します。
- 各訪問先について、最終訪問年を表示します。
- 同一訪問先は 1 行にまとめます。

### 月別訪問先

- 国および都道府県ごと、かつ年度ごとに集計します。
- 各訪問先について、選択年度内の最終訪問月を `YYYY-MM` 形式で表示します。
- 同一年度内の同一訪問先は 1 行にまとめます。
- 生成された HTML 上で年度を選択できるようにします。
- 年度を切り替えると、月別の世界地図、日本地図、表形式の集計結果を選択年度の内容に更新します。

### 件数

レポートには以下の件数を表示します。

- 抽出地点数
- 分類済み地点数
- 年別訪問先数
- 月別訪問先数

## 画面構成

Web page は 2 画面で構成します。

### 1画面目: 入力画面

Timeline JSON を読み込むためのファイル選択ボタンと、分析開始ボタンを配置します。

機能:

- `.json` ファイルを選択できます。
- 未選択時は開始できません。
- 512MB を超えるファイルは警告します。
- 分析中はボタンを無効化し、処理中であることを表示します。
- 通信エラーや API エラーはユーザーに分かりやすく表示します。

### 2画面目: 出力画面

生成された HTML レポートを表示します。

機能:

- 上部に HTML ダウンロードボタンを配置します。
- 下部に生成 HTML を iframe でプレビュー表示します。
- 入力画面へ戻るボタンを配置します。
- 生成 HTML の月別訪問先セクションでは、年度選択ドロップダウンを表示します。

## 出力 HTML レポート

生成される HTML レポートには以下を含めます。

- サマリー
  - 抽出地点数
  - 分類済み地点数
  - 年別訪問先数
  - 月別訪問先数
- 年別訪問先
  - Google Charts GeoChart による世界の白地図表示
  - Google Charts GeoChart による日本の白地図表示
  - 表形式の集計結果
- 月別訪問先
  - Google Charts GeoChart による世界の白地図表示
  - Google Charts GeoChart による日本の白地図表示
  - 年度選択ドロップダウン
  - 表形式の集計結果

訪問済み地域はハイライト表示します。

地図表示には Google Charts の `GeoChart` を利用します。

- 世界地図は ISO 国コードを利用します。
- 日本地図は `JP-xx` の都道府県コードを利用し、`region: JP`、`resolution: provinces` で描画します。
- 未訪問地域は白、訪問済み地域はハイライト色で表示します。
- Google Charts を読み込めない環境では、表形式の結果を主表示として利用します。

## API 仕様

### `POST /api/timeline/analyze`

Timeline JSON を multipart/form-data で受け取り、分析結果と HTML レポートを返します。

リクエスト:

- `file`: `Timeline.json`

レスポンス:

```json
{
  "parsedLocationCount": 23636,
  "classifiedLocationCount": 46646,
  "yearlyVisitCount": 54,
  "monthlyVisitCount": 54,
  "html": "<!doctype html>..."
}
```

エラー:

- ファイル未選択
- JSON 以外のファイル
- 512MB 超のファイル
- JSON 解析失敗
- その他サーバーエラー

## 性能要件

- 115MB 程度の `Timeline.json` を処理できること。
- `sampledata\Timeline.json` を分析できること。
- 国土数値情報 GeoJSON の初回読み込みを許容します。
- 初回読み込み後はキャッシュを利用し、再分析時の応答性能を改善します。

検証実績:

- `sampledata\Timeline.json`
  - 抽出地点数: 約 23,636
  - 分類済み地点数: 約 46,646
  - 分析時間: 約 15 秒台

## Docker 仕様

Docker で起動できるようにします。

```powershell
docker compose -f docker/docker-compose.yml up --build
```

ローカルでは `http://localhost:8080` で利用できます。

## 今後の拡張候補

- 世界各国の分類も行政区域・国境界ポリゴンデータに置き換える。
- Google Charts を利用できないオフライン環境向けに SVG fallback を追加する。
- 単体テスト・統合テストを追加する。
- 出力 HTML に訪問年・訪問月のフィルタ機能を追加する。
- 解析対象を訪問先のみ、移動経路込みなど選択可能にする。
