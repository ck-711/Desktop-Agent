from docx import Document
from docx.shared import Pt, Inches, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.section import WD_SECTION
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.style import WD_STYLE_TYPE
import re, os

SRC = r'C:\Users\K\.codex\attachments\7e269524-c3b7-419d-95cf-4d01fd8491f1\已粘贴的文本.txt'
OUT = r'G:\unity space\Desktop Agent\LLM_VLM_视觉算法_部署优化_面试题与答案.docx'

def parse_source():
    text = open(SRC, encoding='utf-8').read().splitlines()
    sections=[]; current=None
    for line in text:
        s=line.strip()
        if s.startswith('#'):
            h=re.sub(r'^#+\s*','',s)
            if h and not h.startswith('Q') and not h.startswith('至少'):
                current={'title':h,'items':[]}; sections.append(current)
        m=re.match(r'^(\d+)\.\s+(.*)',s)
        if m and current:
            current['items'].append((int(m.group(1)),m.group(2).strip()))
    return sections

DIRECT = {
1:'Transformer 由多层 Encoder 或 Decoder 堆叠而成，核心是多头自注意力、前馈网络、残差连接和归一化；GPT 采用 Decoder-only，并用因果掩码保证只能看见历史 token。',
2:'输入先线性映射为 Q、K、V，计算 softmax(QKᵀ/√dₖ)V；多头结构让模型在不同子空间学习不同依赖关系。',
3:'点积的方差会随 dₖ 增大，除以 √dₖ 可把 logits 控制在稳定范围，避免 softmax 饱和、梯度变小。',
4:'MHA 每个头都有独立 K/V；MQA 让所有 Q 头共享一组 K/V；GQA 让若干 Q 头共享一组 K/V，效果和显存开销折中。',
5:'KV 头数减少后，每个 token 需要缓存的 K、V 张量更小，因此 decode 阶段 KV Cache 显存和带宽访问都下降。',
6:'RoPE 将位置信息编码为二维旋转，Q/K 在不同维度按位置旋转，使内积天然携带相对位置信息，并能较好外推长度。',
7:'LayerNorm 同时减均值并按方差缩放；RMSNorm 只按均方根缩放，不做中心化，计算更简单且常能保持效果。',
8:'SwiGLU 用门控分支调节另一分支的信息流，表达能力强、梯度更平滑，通常以略增参数换取更好的语言建模效果。',
9:'Pre-Norm 把归一化放在子层之前，深层训练更稳定；Post-Norm 放在残差之后，原始 Transformer 常用但深层更难优化。',
10:'Decoder-only 通过因果掩码直接建模下一个 token，训练目标简单、扩展性好，适合统一做生成、对话和工具调用。',
11:'KV Cache 保存已生成前缀在每层注意力中的 K、V，后续 token 只需计算新 token 的 Q/K/V，不必重复计算历史。',
12:'自回归生成每次只新增一个 token；缓存历史 K/V 后，注意力只追加当前 K/V，显著减少重复矩阵计算。',
13:'近似显存 = 层数×2(K/V)×序列长度×KV 头数×每头维度×每元素字节数×batch；还要加权重、激活和框架开销。',
14:'PagedAttention 把 KV Cache 分成固定大小 block 按需分配和回收，减少碎片，支持不同长度请求共享显存并提升批处理利用率。',
15:'上下文越长，KV Cache 与注意力带宽开销近似随长度增长；并发请求会进一步叠加显存压力。',
16:'Prefill 一次处理输入提示词，计算密集、并行度高；Decode 逐 token 生成，访存密集、延迟敏感。',
17:'TTFT 是首 token 延迟；TPOT 是每个后续 token 平均延迟；TPS 是每秒生成 token 数。三者分别反映首响、流式速度和吞吐。',
18:'GPU 负责矩阵计算和大部分模型推理；CPU 负责请求调度、tokenize、数据搬运、采样和部分后处理，瓶颈取决于流水线。',
19:'预训练学习通用知识，SFT 学习指令格式和任务行为，RLHF/偏好优化让输出更符合人类偏好、安全和风格。',
20:'LoRA 冻结基座权重，只训练低秩增量 ΔW=BA，参数量、梯度和优化器状态都大幅减少。',
21:'r 是低秩矩阵的秩，控制容量；alpha 是缩放系数，实际常用 alpha/r 调节增量强度。',
22:'LoRA 通常在 FP16/BF16 基座上训练适配器；QLoRA 将基座量化到 4-bit，并以低精度权重加 LoRA 训练，显存更低。',
23:'QLoRA 前向时把 4-bit 权重反量化到计算精度，梯度只更新 LoRA 参数，因此不需要更新量化基座的权重。',
24:'NF4 针对正态分布权重设计非均匀量化码本，比均匀 INT4 更充分利用权重分布，通常精度更好。',
25:'数据要覆盖真实任务、边界和拒答样例；统一指令/回答格式，去重去脏，划分训练验证集并检查长度分布和泄漏。',
26:'对回答 token 计算交叉熵，通常将 prompt 部分 label 置为 -100，只让模型学习目标回答；按有效 token 做平均。',
27:'训练数据分布过窄或学习率/轮数过大，会让模型过度拟合新任务而遗忘通用能力；可混入通用数据、降低学习率和训练轮数。',
28:'DPO 直接用偏好对优化 chosen 与 rejected 的相对概率；PPO 需要策略模型、价值模型和奖励模型，并通过在线采样更新。',
29:'DPO 把奖励模型的最优解代入偏好目标，直接用离线偏好对训练，因此不需显式训练和部署 Reward Model。',
30:'GRPO 用同一问题采样一组答案，以组内相对奖励估计优势，省去独立价值网络；PPO 通常需要 Critic。',
31:'已有稳定知识且需可追溯时优先 RAG；需要改变格式、风格或稳定行为时用 SFT；两者常组合。',
32:'降低学习率和训练轮数、加入通用回放数据、冻结更多层、减小 LoRA 容量，并用混合数据和基准集监控遗忘。',
33:'Agent 是能感知任务、规划步骤、调用工具并根据结果迭代的 LLM 系统，而不是一次性生成文本。',
34:'Workflow 的路径通常预先写死；Agent 可根据状态和工具结果动态选择下一步，具有更强适应性但更难控。',
35:'ReAct 将 Reasoning 与 Acting 交替：先产生行动意图，再调用工具，读取观察结果后继续决策。',
36:'模型输出结构化函数名和参数，运行时校验 schema、执行函数并把结果回传消息，模型再决定是否继续。',
37:'Tool Calling 是协议层的结构化调用；ReAct 是更宽泛的思考-行动循环，可用自然语言或工具调用实现。',
38:'LangGraph 用有向图和持久化 State 表达节点、边、循环、重试和人工介入，比单链式编排更适合复杂 Agent。',
39:'State 是贯穿图执行的共享上下文，例如消息、任务、工具结果、重试次数和权限信息；节点读取并返回增量更新。',
40:'让条件边根据 State 把执行路由回规划或工具节点，并设置终止条件和最大步数。',
41:'设置最大轮数、超时、重复动作检测、预算和状态机终止条件；对相同工具参数做幂等去重。',
42:'按任务复杂度和延迟预算设置，通常 3-10 轮并配合 token/时间预算；超限要返回可解释的部分结果。',
43:'区分可重试错误与业务错误；指数退避重试有限次数，记录错误上下文，必要时切换工具或请求人工介入。',
44:'不能无条件重试。先验证参数和错误类型，最多有限次；连续失败应改变策略或快速失败，避免放大成本。',
45:'工具白名单、参数 schema、权限与租户校验、最小权限凭证、审批节点和审计日志共同构成防线。',
46:'多 Agent 增加通信、状态同步、调度和失败组合复杂度，可能重复劳动和相互矛盾；只有职责边界清晰时才值得使用。',
47:'先由 Supervisor 做意图识别和任务拆解，再把子任务路由给专长 Agent，最后汇总并验证结果。',
48:'Supervisor Agent 负责分派、收集结果、处理失败和决定终止，不直接承担所有领域细节。',
49:'用显式共享 State 或事件总线传递任务、结果和元数据；避免各 Agent 私自维护不可见状态。',
50:'为任务设置唯一 ID、幂等键、状态锁和完成标记；Supervisor 在汇总前检查覆盖率和重复执行。',
51:'常见有工作记忆、对话历史、情节/事件记忆、语义记忆、用户画像和程序记忆。',
52:'短期记忆服务当前任务、生命周期短；长期记忆跨会话保存稳定事实和偏好，需要检索、版本和权限。',
53:'不应全部塞入 Prompt；应按相关性、时间和可信度检索，摘要旧内容并保留必要原文。',
54:'在规划前读取与当前任务相关的记忆，在生成或工具调用前注入最小必要上下文。',
55:'用户明确表达稳定偏好、任务结论或可复用事实时写入；临时闲聊通常不写。',
56:'新信息与旧记忆冲突、置信度更高或用户明确更正时更新，并保留时间戳和来源。',
57:'用户要求删除、过期、被证伪或违反保留策略时删除；删除要覆盖索引、缓存和备份。',
58:'当前任务依赖且必须一致的记忆读取、权限判断应同步；批量摘要、索引和统计可异步。',
59:'把记忆压缩成带来源、时间、置信度和有效期的事实；合并重复项，保留可追溯原文链接。',
60:'以稳定性、复用价值、明确性、敏感度和用户授权打分，超过阈值才写入长期记忆。',
61:'更多不等于更好；无关或过时记忆会增加噪声、成本和错误引用。',
62:'分域隔离、来源和置信度标注、冲突检测、用户可查看/删除、敏感信息脱敏可避免污染。',
63:'写入新版本并让最新且可信的事实优先，同时记录变更历史；不应静默覆盖不可审计的数据。',
64:'用用户/会话 ID 做命名空间，检索相关记忆后拼入上下文，写回经过验证的稳定结论。',
65:'RAG 流程包括解析、切分、向量化、索引、召回、重排、组装 Prompt、生成和引用校验。',
66:'Chunk 让检索粒度与上下文窗口可控，并尽量保持语义完整和标题层级。',
67:'过大导致主题混杂、召回不精准且 Prompt 浪费；过小会丢失上下文、命中片段无法独立回答。',
68:'Dense 用语义向量相似度，Sparse 用词项/倒排统计；混合检索兼顾同义表达和精确关键词。',
69:'BGE-M3 支持多语言、多粒度和 dense/sparse/multi-vector 表达，适合混合召回。',
70:'HyDE 先让模型生成假设性答案，再用其向量检索真实文档，改善短查询的语义匹配，但要防止幻觉偏移。',
71:'RRF 按各检索结果的名次累加 1/(k+rank)，无需校准不同检索器分数即可融合。',
72:'Reranker 对候选 query-document 做更深的交互打分，计算贵，所以放在较小的召回集合之后。',
73:'Embedding 负责大规模近似召回；Reranker 负责候选精排，输入是查询和文档对而非单独向量。',
74:'可能因为切分、元数据、查询歧义、向量模型或索引过滤错误；要检查召回日志、相似度和覆盖率。',
75:'正确片段若未进入 Prompt、上下文截断、指令冲突或模型本身推理失败，都会出现召回正确但回答错误。',
76:'离线看 Recall@k、MRR、NDCG 和命中率；在线看 groundedness、引用准确率、答案正确率、延迟和成本。',
77:'Recall 是相关项被召回比例，Precision 是召回项中相关比例，MRR 看首个相关结果排名，NDCG 考虑多级相关性和排序。',
78:'按标题/语义递归切分，保留父子关系；长文档可先摘要或分层检索，再对命中段落重排。',
79:'用结构化解析保留表头、行列和单位；必要时把表格转成 Markdown/JSON 并保存页码和坐标。',
80:'先 OCR 或视觉解析提取图片文字和布局，再将图像描述、坐标、页码作为可检索元数据。',
81:'VLM 在语言模型上接入视觉编码器和投影层，可处理文本之外的图像/视频 token。',
82:'图像经 Vision Encoder 转成 patch 特征，再由 projector 映射到 LLM 的词向量空间，与文本 token 一起输入。',
83:'Vision Encoder 提取局部和全局视觉语义，常见是 ViT/CLIP；LLM 负责跨模态对齐、推理和生成。',
84:'CLIP 用图文对比学习把匹配的图像和文本拉近、不匹配的推远，得到共享语义空间。',
85:'图像 patch 特征维度通常不同于语言 embedding，视觉投影层负责维度、分布和语义空间对齐。',
86:'分辨率提高会增加 patch 数量，注意力和投影计算量随 token 数增长，显存也上升。',
87:'通过 cross-attention 或把视觉 token 插入文本序列，模型在同一上下文中建立图文对应关系。',
88:'OCR 擅长高精度文字识别；VLM 擅长布局、关系和开放语义理解。文字清晰且需逐字准确时优先 OCR，复杂场景可组合。',
89:'视频需要处理时间维度，可采样关键帧、做时序聚合或使用视频编码器；全量帧会带来 token、显存和延迟爆炸。',
90:'CNN 的局部平移不变性和权重共享使其适合提取图像局部模式；卷积核在滑窗位置做加权求和。',
91:'Stride 控制卷积窗口移动步长；Padding 在边缘补值以控制输出尺寸并保留边界信息；Pooling 下采样并增强局部不变性。',
92:'残差连接学习增量 F(x)，让梯度可沿捷径传播，缓解深层网络退化；BatchNorm 用批统计量稳定训练。',
93:'YOLO 通常由 Backbone 提特征、Neck 融合多尺度信息、Head 在各尺度预测类别、框和置信度。',
94:'检测头回归中心、宽高等框参数并输出类别概率；解码后按置信度和类别过滤。',
95:'IoU 是预测框与真值框交集面积除以并集面积，用于匹配、损失和评估。',
96:'NMS 按分数排序，保留最高分框并抑制与其 IoU 过高的重复框，避免同一目标多次计数。',
97:'Precision=TP/(TP+FP)，Recall=TP/(TP+FN)；mAP 是各类别 AP 的平均，AP 是某一类别 PR 曲线下面积。',
98:'按 IoU 阈值与类别匹配正样本，未匹配或背景为负样本；现代 YOLO 还结合中心先验和动态分配。',
99:'YOLO 单阶段、共享特征、直接回归并行预测，省去候选区域生成，因此速度快。',
100:'Tracking 在帧间为同一目标维持稳定 ID；Detection 只回答当前帧有哪些目标及其位置。',
101:'Kalman Filter 用运动模型预测下一状态，再用检测观测校正位置和速度，并维护不确定度。',
102:'Hungarian Algorithm 在代价矩阵上求全局最小匹配，用于将轨迹与检测框一一关联。',
103:'ByteTrack 先用高置信度框匹配，再把低置信度框用于补充未匹配轨迹，最后更新和新建轨迹。',
104:'低分框可能来自遮挡目标，虽然分类置信度低但位置仍有用；利用它们能减少轨迹断裂和 ID Switch。',
105:'DeepSORT 加 ReID 外观特征，ByteTrack 主要依赖 IoU/运动和双阈值关联，后者更轻量、无需额外 ReID。',
106:'遮挡时依靠 Kalman 预测维持轨迹，遮挡结束后用检测重新关联；可结合 ReID 和更长轨迹保留时间。',
107:'ID Switch 指同一真实目标在跟踪过程中被分配了不同 ID，常由遮挡、交叉或关联失败引起。',
108:'常见指标包括 MOTA、IDF1、HOTA、ID Switch、FP/FN；要同时衡量检测准确性和身份一致性。',
109:'ONNX 是跨框架的计算图和权重交换格式；PyTorch 导出后可由多种运行时执行。',
110:'ONNX Runtime 提供统一推理 API，Execution Provider 把算子分发到 CUDA、TensorRT、CPU、DirectML 等后端。',
111:'TensorRT 可作为 ONNX Runtime 的 EP，也可直接构建 Engine；它做图优化、层融合、内存规划和硬件特化。',
112:'FP32 精度高但慢且占显存；FP16 减半存储、通常损失小；INT8/INT4 更省但需校准且精度风险更高。',
113:'量化用更少 bit 表示权重/激活，减少显存和带宽，代价是舍入和离群值导致的误差。',
114:'PTQ 在训练后用校准数据估计量化参数；QAT 在训练中模拟量化，让模型主动适应误差。',
115:'INT8 部署通常包含收集代表性数据、校准 scale/zero-point、选择对称或非对称方案、验证精度并构建 engine。',
116:'Calibration 用代表性输入统计激活范围或分布，决定每层量化参数，数据覆盖不足会导致误差。',
117:'算子融合把连续算子合并成一个更大的 kernel，减少中间张量读写和 kernel launch。',
118:'CPU/GPU 瓶颈常来自数据拷贝、预处理、未支持算子回退、同步点或 batch 不合适；应先 profile 再优化。',
119:'可用更小输入、FP16/INT8、TensorRT、融合、异步流水线、固定 shape 和预分配内存降低端侧延迟。',
120:'减少 batch、量化、剪枝、降低分辨率、减少 KV Cache、权重共享和分层 offload 都可降显存。',
121:'提高 batch/并发、动态 batching、CUDA Graph、KV Cache、算子融合和请求调度可提升吞吐。',
122:'先用 profiler 定位是预处理、H2D、算子、后处理还是同步，再做单变量 benchmark，避免凭感觉优化。',
123:'TensorRT 针对 NVIDIA GPU 做图优化和 kernel 自动选择，通常比通用运行时更低延迟。',
124:'FP16 适合硬件支持半精度且精度敏感的场景；INT8 需要校准，吞吐和显存更优但误差更难控。',
125:'Engine 是针对具体 GPU、输入 shape 和精度构建的序列化推理计划，部署前构建、运行时加载。',
126:'TensorRT 会做常量折叠、层/Kernel 融合、精度选择和内存复用。',
127:'RKNN 是 Rockchip NPU 的模型转换与运行工具链，能把 ONNX 等模型编译为芯片友好的格式。',
128:'NCNN 是轻量跨平台推理框架，CPU/移动端友好；TensorRT 更偏 NVIDIA GPU，硬件特化更强。',
129:'基本流程是 PyTorch→ONNX→检查算子与动态维度→TensorRT builder→校准/选择 FP16/INT8→序列化 Engine→验证。',
130:'ONNX→RKNN 还需检查 NPU 支持算子、设置 mean/std 和量化数据集，转换后在目标板上做精度与性能回归。',
131:'先查不支持算子和日志，能替换就改图/插件；否则拆分子图、回退到 CUDA/CPU 或实现自定义 plugin。',
132:'按预处理、算子、后处理和量化校准逐层对比 FP32 基线，定位误差突然增大的层，再做混合精度或 QAT���',
133:'PyTorch→端侧通常是导出 ONNX、验证数值一致性、选择 EP/后端、量化优化、封装前后处理并在真机 benchmark。',
134:'GGUF 适合 llama.cpp 生态的端侧量化与内存映射，文件自包含、加载快且 CPU/GPU 混合执行方便。',
135:'Q4_K_M 是 4-bit K-quant 的中等质量配置，对敏感层保留更高精度，在大小、速度和效果间折中。',
136:'4-bit 量化把权重映射到约 16 个离散值，并按 block 保存 scale/zero-point；实际误差取决于分组和离群值。',
137:'GGUF 面向自回归 LLM 权重与 tokenizer 元数据；ONNX 是通用计算图格式，适合多种模型和硬件后端。',
138:'llama.cpp 依赖少、支持内存映射、量化 kernel 和 CPU/GPU offload，适合桌面与端侧本地推理。',
139:'TTFT 从提交请求到收到首个生成 token 计时；应固定 prompt、预热并区分 tokenize、prefill 和网络/队列时间。',
140:'tokens/s=生成 token 数/生成耗时，应在稳定 decode 区间统计并多次取均值或分位数。',
141:'TTS 的 RTF=音频生成耗时/音频时长，RTF<1 表示快于实时。',
142:'端到端延迟还包括排队、解码、分句、TTS 首包和播放缓冲；LLM 与 TTS 可通过流式分句和并行流水线重叠。',
143:'CancellationToken 让生成、网络和 TTS 在用户打断时及时取消，释放线程/GPU并降低尾部延迟。',
144:'控制上下文、量化权重、KV Cache、并发和临时 buffer；用 profiler 分解显存峰值而不是只看模型文件大小。',
145:'感知延迟来自 TTFT、流式输出、首段 TTS 和并行播放，不等于所有文本生成和音频完成时间之和。',
146:'先明确跨模态/视觉基础可迁移能力，再补足检测、跟踪、部署链路；用项目中真实指标证明工程落地能力。',
147:'Transformer 擅长全局关系和长程建模，CNN 擅长局部归纳偏置和高效视觉特征；现代视觉模型常结合二者。',
148:'视频中每帧由 YOLO 产生检测框，ByteTrack 用运动预测和 IoU 匹配跨帧关联，维护稳定 track ID。',
149:'低置信度框可能是遮挡目标的弱观测，第二阶段利用它们能恢复轨迹而不显著引入误匹配。',
150:'ONNX Runtime 通用、EP 多；TensorRT 更专注 NVIDIA 的极致优化，通常需针对硬件构建 engine。',
151:'精度与速度按 FP16/INT8/INT4 递减的存储和计算成本排序；位宽越低越省资源，但越需要校准、敏感层保护和验证。',
152:'用权限、白名单、schema、超时、最大步数、��等键、审计和人工审批共同防死循环及越权。',
153:'PyTorch→ONNX→算子/动态维度检查→RKNN 转换与量化→板端运行→对齐精度、延迟和内存；不支持算子则改图或回退。',
154:'优先讲清目标、约束和指标，再按 profiling、结构/精度、运行时、流水线、真机回归闭环回答。',
155:'选择 GGUF 是因为它与 llama.cpp/LLMUnity 端侧生态契合，支持量化、内存映射和 CPU/GPU offload。',
156:'Q4_K_M 在 4-bit 体积和质量间平衡，通常比纯低位量化更稳，适合显存受限的本地助手。',
157:'4-bit 量化把每个权重用约 4 bit 表示，并按块保存缩放参数；显存和带宽大幅下降，但会有近似误差。',
158:'GGUF 是量化 LLM 的文件与元数据格式；ONNX 是通用计算图交换格式，二者服务的模型生态不同。',
159:'LLMUnity 通常封装本地模型加载、采样和流式回调；面试时应结合实际版本说明底层是否调用 llama.cpp。',
160:'llama.cpp 原生 C/C++、依赖少、量化成熟并支持多种后端，适合端侧部署。',
161:'TTFT 从请求发出到首 token 到达回调的单调时钟差值，需预热并报告 p50/p95。',
162:'30 tokens/s=稳定 decode 阶段生成 token 数除以耗时，排除首 token 和 UI/网络时间。',
163:'RTF=生成音频耗时/音频时长；越小越快，RTF<1 才能实时播放。',
164:'端到端还包含队列、分句、首包、缓冲与播放；采用流式 LLM、分句 TTS 和并行可以重叠这些阶段。',
165:'取消 token 传播到生成器、队列和 TTS，停止无用计算并清理资源，降低打断后的恢复时间。',
166:'用量化模型、限制上下文和 KV Cache、控制并发、复用 buffer，并用显存 profiler 验证峰值。',
167:'感知延迟来自首 token/首音频和并行流水线；用户开始听到声音前不必等待完整回答。',
168:'可按需补充 benchmark 表，明确硬件、模型、量化、输入长度、并发、p50/p95 和测试脚本，保证指标可复现。',
}

def answer(n,q):
    if n in DIRECT: return DIRECT[n]
    # fallback for any missed source item
    return '面试回答要点：先定义概念，再说明核心机制、适用场景、主要权衡，并结合一个可验证的工程指标或排障步骤。'

def set_cell_shading(cell, fill):
    tcPr=cell._tc.get_or_add_tcPr(); shd=OxmlElement('w:shd'); shd.set(qn('w:fill'),fill); tcPr.append(shd)

def main():
    sections=parse_source()
    doc=Document(); sec=doc.sections[0]; sec.top_margin=Inches(.65); sec.bottom_margin=Inches(.65); sec.left_margin=Inches(.72); sec.right_margin=Inches(.72)
    styles=doc.styles
    styles['Normal'].font.name='Microsoft YaHei'; styles['Normal']._element.rPr.rFonts.set(qn('w:eastAsia'),'Microsoft YaHei'); styles['Normal'].font.size=Pt(9.5)
    for name,size,color in [('Title',22,'000000'),('Heading 1',15,'000000'),('Heading 2',11.5,'1F4E79')]:
        st=styles[name]; st.font.name='Microsoft YaHei'; st._element.rPr.rFonts.set(qn('w:eastAsia'),'Microsoft YaHei'); st.font.size=Pt(size); st.font.bold=True; st.font.color.rgb=RGBColor.from_string(color)
    title=doc.add_paragraph(style='Title'); title.alignment=WD_ALIGN_PARAGRAPH.CENTER; title.add_run('LLM VLM 视觉算法与部署优化面试题与答案')
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('面向多模态算法与端侧 AI 岗位的系统复习手册'); r.italic=True; r.font.color.rgb=RGBColor(89,89,89)
    doc.add_paragraph('本手册将简历中涉及的 LLM、Agent、RAG、VLM、传统视觉、目标跟踪、模型部署和端侧项目，整理为 168 道高频面试题及可直接口述的回答。建议先按优先级复习部署与视觉基础，再用项目题验证是否能把原理、工程取舍和指标连起来。')
    doc.add_heading('复习优先级',1)
    doc.add_paragraph('① ONNX / TensorRT / INT8/INT4  ② YOLO / ByteTrack  ③ Transformer / KV Cache  ④ Agent  ⑤ RAG  ⑥ VLM。每道题的答案控制在面试首轮可讲清的长度，追问时再补公式、实现细节和实验数据。')
    doc.add_heading('目录',1)
    for s in sections: doc.add_paragraph(s['title'], style='List Bullet')
    for s in sections:
        doc.add_page_break(); doc.add_heading(s['title'],1)
        for n,q in s['items']:
            p=doc.add_paragraph(style='Heading 2'); p.paragraph_format.space_before=Pt(7); p.paragraph_format.space_after=Pt(2); p.add_run(f'{n}. {q}')
            a=doc.add_paragraph(); a.paragraph_format.left_indent=Inches(.15); a.paragraph_format.space_after=Pt(5); rr=a.add_run('答案：'); rr.bold=True; a.add_run(answer(n,q))
    doc.add_page_break(); doc.add_heading('最后十道连环问的答题框架',1)
    for n in range(146,155):
        if n in DIRECT:
            doc.add_paragraph(f'{n-145}. {answer(n,"")}', style='List Number')
    doc.add_paragraph('答题时优先给结论，再给机制、取舍和验证方法；如果题目涉及部署，务必说明硬件、精度、延迟、显存和精度回归指标。')
    # footer
    for section in doc.sections:
        f=section.footer.paragraphs[0]; f.alignment=WD_ALIGN_PARAGRAPH.CENTER; f.add_run('面试复习手册').font.size=Pt(8)
    doc.save(OUT)
    print(OUT)

if __name__=='__main__': main()
