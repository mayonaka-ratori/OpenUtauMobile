# Roadmap v2 → v3 Crosswalk

**Note:** No standalone `roadmap-v2.md` file exists in this repository. The v2 item IDs referenced in this document are reconstructed from the v3 roadmap's own "優先度変更まとめ（v2 → v3）" section (lines 146–175 of `roadmap-v3.md`), which lists v2 items by their implicit IDs. This crosswalk is therefore derived, not a direct file-to-file diff.

---

## Mapping: v2 item → v3 item

| v2 ID (implied) | v2 Title (reconstructed) | v3 ID | v3 Title | Change Type | Scope Delta |
|:----------------|:------------------------|:------|:---------|:------------|:------------|
| v2/0-1 | オートセーブ | **0-2** | オートセーブ + セッション復元 + セーフモード | Expanded | Added: background-transition save, atomic write (temp→rename), next-launch recovery UI, render-disabled safe-mode launch |
| v2/0-2 | (implied: crash defense / stability) | **0-1** | クラッシュ・性能テレメトリ基盤 | **New/Promoted** | Entirely new item. Sentry/Crashlytics opt-in, OOM detection, startup/render timing, cache metrics. v2 had no equivalent. |
| v2/0-3 | cutoff防御 | **0-4** | cutoff 防御 + 修正候補提示 | Expanded | Added: import-time entry listing, sample name, estimated cause, recommended correction value |
| v2/0-4 | (implied: OTO / singer compat) | **0-3** | 音源インポート時の互換性診断 | **New/Promoted** | Entirely new item elevated to P0. Full validator: alias dupes, cutoff/offset/preutter/overlap anomalies, char encoding estimation, memory estimation, use/caution/incompatible verdict |
| v2/0-5 | メモリ管理・クラッシュ低減 | **0-5** | メモリ管理改善・クラッシュ頻度低減 | Maintained | Same intent. v3 adds explicit mention of ArrayPool, multi-res waveform, piano roll virtualization |
| v2/0-6 | レンダリングキャッシュ管理 (basic LRU) | **0-6** | レンダリングキャッシュ管理 | Expanded | Added: cache key must include singer hash, expression values, resampler settings, tempo map — prevents stale audio |
| v2/0-7 | (not present in v2) | **0-7** | 資産レジストリ / パス抽象化 | **New/Promoted** | Entirely new item elevated to P0. Internal ID + URI persistence + project-package-ready design |
| v2/0-8 | (not present in v2) | **0-8** | Expressionパラメータ編集の動作修正 | **New** | New P0 item covering touch binding, scroll conflict, draw update inconsistency |
| v2/0-9 | (not present in v2) | **0-9** | レンダリングジョブ管理基盤 | **New/Promoted** | New P0 item. Debounce, job cancel on consecutive edit, visible-range priority, selection-range priority |
| v2/0-10 | (not present in v2) | **0-10** | ライセンス / 配布ポリシー整備 | **New** | New P0 item. Legal/policy groundwork for demo singer, ONNX models, resampler |
| v2/1-4 | ノート長の数値入力 (length only) | **1-5** | ノート長・開始位置・音高の数値入力 | Expanded | v3 extends to start tick, pitch, lyric, and all major parameters — not just duration |
| v2/1-8 | BGM読み込み安定化 | **1-7** | BGM安定化 + 圧縮音源対応 | Expanded | Added: mp3/m4a/AAC/ogg; internal conversion to unified format |
| v2/1-9 | (not present in v2) | **1-9** | 適応レイアウト + タブレット / スタイラスUI基盤 | **New/Promoted** | New P1 item elevated above UI theme. Covers phone portrait/landscape, tablet, split-screen, Pencil/S-Pen, minimum hit targets |
| v2/1-10 | デモ音源同梱 | **1-1** | 初回体験の導線（デモシンガー + サンプルプロジェクト + チュートリアル） | Merged + Promoted | v2/1-10 (demo singer) + v2/X-1 (tutorial) merged into a single 5-step onboarding item at higher priority |
| v2/2-2 | Luaフォネマイザ | **2-3** | カスタムフォネマイザ（Lua / 宣言的DSLの二段構え） | Expanded | Added JSON/DSL fallback for App Store safety (Lua sandboxing concerns). MoonSharp validation continues. |
| v2/3-1 | SideStore/AltStore対応 | **3-2** | iOS配布戦略整備 | Deprioritized + Renamed | SideStore is now one option among several (TestFlight, AltStore, future App Store). No longer the primary iOS strategy. |
| v2/3-4 | AIピッチ生成（full auto） | **3-5 + 2-10** | 簡易AI自動ピッチ（3-5） preceded by 非AI補助層（2-10） | Restructured | v2 aimed for full AI pitch directly. v3 inserts a non-AI template/preset layer (2-10) before full AI (3-5). Better risk management. |
| v2/X-1 | チュートリアル | **1-1** | 初回体験の導線 | Merged into P1 | Merged with demo singer item and promoted to Phase 1 |
| v2/2-4 | シンガー遅延ロードと切替 | **2-1** | シンガーの遅延ロードとOTOオンデマンド展開 | Maintained | v3 clarifies: not just upper limit removal, but "only expand phonemes actually used" |

---

## Items New in v3 (no v2 equivalent)

| v3 ID | Title | Rationale per v3 |
|:------|:------|:-----------------|
| 0-1 | クラッシュ・性能テレメトリ | Memory improvement and unreproducible bugs need observability first |
| 0-3 | 音源インポート時の互換性診断 | Reactive cutoff defense (0-4) is insufficient; proactive import scan reduces support load |
| 0-7 | 資産レジストリ / パス抽象化 | Cloud sync and SAF cannot be built on top of absolute paths; this must come first |
| 0-8 | Expressionパラメータ編集修正 | Identified as a blocking usability issue in v3 |
| 0-9 | レンダリングジョブ管理基盤 | Preview experience and battery life core dependency |
| 0-10 | ライセンス整備 | Legal prerequisite before public distribution |
| 1-3 | 歌詞→発音プレビュー可視化 | New; helps both beginners and advanced users debug phonemization |
| 1-9 | 適応レイアウト / タブレットUI基盤 | Elevated from implied to explicit; ranks above UI theming |
| 2-10 | 簡易ピッチ補助（非AI） | New intermediate step between manual pitch and full AI |
| X-8 | 回帰テスト用サンプル群 | New; CI/CD health |
| X-9 | 端末互換性マトリクスと性能モード | New; addresses low-RAM device reality |
| X-10 | サポートバンドル出力 | New; pairs with 0-1 telemetry for bug reproduction |

---

## Items Deprioritized in v3

| v2 ID (implied) | Item | v3 Status | Reason |
|:----------------|:-----|:----------|:-------|
| v2 theme item | UIテーマ / カラー | **2-9** (deferred) | Adaptive layout (1-9) must come first |
| v2/3-1 | SideStore / AltStore | **3-2** (demoted) | Niche audience; not the core winning strategy |
| v2/3-4 | Full AI自動ピッチ即時実装 | **3-5** (deferred behind 2-10) | Template/assist layer has better ROI at this stage |
| v2 ENUNU | ENUNUサポート | **3-7** (still low) | High cost, low return; depends on DiffSinger/OpenUtau mainline alignment |
