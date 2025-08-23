
## 前提環境

* .NET 9.0

## 概要

このリポジトリは、特定ロールを所有しているユーザーのプロフィール保存とボイスチャットの参加時間を計測するBotです。以下の機能を提供します。

全ての機能は、指定されたロールのユーザーに対してのみ有効です。

* ボイスチャンネルの参加時間(最大過去12か月まで確認可能)
* ボイスチャンネルの参加履歴(全ユーザー共有で直近10件まで確認可能)
* ロールの確認
* プロフィールの設定・確認

なお、これらの情報は、`http://localhost:8080`で確認できます。

コンフィグで、`Kestrel:Endpoints:Http:Url`を変更することで、ポートやIPアドレスを変更できます。

※ readmeに書いてある設定(http://0.0.0.0:8080)では、使っている端末のIPアドレスでもアクセス可能です。


ただし、DiscordEventServiceで説明に使っているPortを8080に固定しているため、Port変更する場合は、`DiscordEventService.cs`の`OnSlashCommandExecuted`の`8080`と記述されている部分を変更する必要があります。

`DiscordEventService.cs`の`OnSlashCommandExecuted` : https://github.com/link1345/LinkClanBot3/blob/d8eb9cf69f87e5faac24116e5297c1dab6cff4fe/LinkClanBot3/Discord/DiscordEventService.cs#L406-L477

### 注意
このリポジトリは、Volというdiscordのコミュニティで使用することを前提として作られているため、一部の名称が、`Vol`と付いています。

ですが、機能や構成は、他のdiscordコミュニティでも使用可能なものになっています。

`Vol`という名称は、ソースを検索して簡単に変更可能です。

#### 現時点での`Vol`と記述された場所

* https://github.com/link1345/LinkClanBot3/blob/master/LinkClanBot3/Shared/MainLayout.razor
* https://github.com/link1345/LinkClanBot3/blob/master/LinkClanBot3/Pages/Shared/_Layout.cshtml
* https://github.com/link1345/LinkClanBot3/blob/master/LinkClanBot3/Pages/Index.razor
* https://github.com/link1345/LinkClanBot3/blob/master/LinkClanBot3/Shared/NavMenu.razor

## ビルド

```bash
git clone https://github.com/link1345/LinkClanBot3.git
```

cloneしたソリューションのLinkClanBot3.slnを開いて、Releaseでビルドしてください。

その後、`LinkClanBot3\bin\Release\net9.0\`フォルダ一式を、実行したい環境に移動させます。

## コンフィグ設定(実環境での実行時)

```json
{
  "DatabaseConnection": "ConnectionStrings:DatabaseConnection",
  "DiscordToken": "** token **",
  "MessageChannel": "** text channel **",
  "VoiceChannle:AdminRole:0": "** role **",
  "VoiceChannle:LeaderRole:0": "** role **",
  "VoiceChannle:MemberRole:0": "** role **",
  "VoiceChannle:TemporaryMemberRole:0": "** role **",
  "ConnectionStrings:DatabaseConnection": "Data Source=** path **/SQlLiteDatabase.db",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:8080"
      }
    }
  }
}
```

上記のような内容の`appsettings.Production.json`を作成してください。

* DiscordToken: DiscordのBotトークンを指定します。
* MessageChannel: Botがメッセージを送信するテキストチャンネルを指定します。
* VoiceChannle: 各種ロールを指定します。
    * AdminRole: Botの管理者ロールを指定します。
    * LeaderRole: Botのリーダーロールを指定します。
    * MemberRole: Botのメンバーロールを指定します。
    * TemporaryMemberRole: Botの一時メンバーロールを指定します。
* ConnectionStrings: DatabaseConnection: SQLiteのDBファイルのパスを指定します。

## 環境変数設定

```bash
export DOTNET_ENVIRONMENT=Production
```

## DBファイル生成実行

本リポジトリは、SQLiteを使用しています。なので、DBファイルを生成する必要があります。

```bash
dotnet ef database update
```

このコマンドを実行することで、`SQlLiteDatabase.db`が生成されます。

## 実行

```bash
./LinkClanBot3 
```