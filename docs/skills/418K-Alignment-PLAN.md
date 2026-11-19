# 4–18K 技能引擎对齐记录

> **状态（2026-11-20）：引擎侧 4–18K 支持已实现。** 本文前半记录已落地的实现，后半列出仍未覆盖的下游工作；早期方案不再是当前实施计划。

## 当前实现

- `IEzMsdEngine` / `EzNKeyMsdEngine` 提供统一的技能计算入口，`SupportsKeyCount` 是引擎能力判断的唯一来源。
- MSD 与 SSR 共享由 `EzCalcNote` 表示的 note-array 路径；4–18K 支持由嵌入式 n-key MinaCalc WebAssembly 引擎提供，并由 Wasmtime 宿主执行。旧的 `.osu` 文本解析分支和 MinaCalc 0.4.2 限制已退出当前路径。
- 引擎级测试覆盖支持范围、非支持键数、输入转换和错误恢复；下游模式/键数覆盖仍见 [DATA-FOLLOWUPS.md](./DATA-FOLLOWUPS.md)。
- 超出 4–18K 的键数不由引擎处理。17/18 列过宽和弦是已知模块限制，异常引擎实例会被丢弃重建；参见待办中的已知限制。
- 图表段位映射和玩家段位收录并未随引擎支持范围自动扩展。5K、8–18K 图表段位使用 4K 区间表回退；10K 以上尚未进入 tracked skillset / filing / ladder 范围。

## 尚未完成的下游项

以 [DATA-FOLLOWUPS.md](./DATA-FOLLOWUPS.md) 的状态为准：

- `DATA-PatternAxis-KeymodeSet`：重新推导 5K、9K 与 10–18K 的 pattern 轴集合。
- `DATA-Skillset-Dan-Coverage-10K+`：决定并实现 10–18K 的 skillset 段位收录、分桶和阶梯覆盖。
- 把代表性键数的图表分析、段位估算和玩家聚合端到端覆盖补齐；现有引擎级测试不能替代这些消费端验证。
- 独立 DLL / 项目提取仍是可选的未来方向，不是当前支持 4–18K 的前置条件。

## 范围与验证边界

技能链包括图表 MSD/SSR、图表元数据与段位、玩家聚合，以及 skillset filing / buckets / ladders。变更引擎输入转换或聚合语义时，需评估算法版本和派生数据失效范围；具体规则见 [Skills-README.md](./Skills-README.md)。

提交新键数支持时，同时检查引擎路由、图表分析、段位映射、玩家聚合和对应测试。不要再添加硬编码 4/6/7K 引擎分支；需要键数能力判断时使用 `IEzMsdEngine.SupportsKeyCount`。

## 历史方案

本文件早期关于替换 MinaCalc、接入 `.osu` 文本路径、初建引擎抽象及“5K / 8K+ 尚被引擎阻塞”的条目已经过期。现行引擎和仍开放的消费端事项以源码、测试和 [DATA-FOLLOWUPS.md](./DATA-FOLLOWUPS.md) 为准。
