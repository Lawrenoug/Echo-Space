# Echo Space Banana 美术资源提示词

这份文档用于整理 `Echo Space` 的美术资源生成提示词，目标是让你可以直接复制英文 prompt 到 Banana 或类似图片生成工具里使用。

统一风格约束：
- 项目气质：暗幻想 2D 横版动作游戏，采用“余烬与回声”方向。
- 现实世界强调色：低饱和余烬橙。
- 灵魂世界强调色：冷青色灵魂光。
- 基础色板：炭黑、低饱和蓝灰、风化石材、锈蚀金属。
- 全局避免：可爱卡通、Q 版、手游休闲风、过量赛博霓虹、高饱和彩虹、水印，以及非明确要求的内嵌文字。

需要时可追加到 Banana prompt 前面的通用说明：
```text
Generate a clean production game asset, not a concept mood board. Keep the asset readable at game scale. Use a transparent background when requested. Do not add logos, captions, watermarks, mockup frames, UI text, or extra decorative labels.
```

## 1. TileSet / TileMap 贴图

生成后的推荐保存路径：
- `res://Assets/Environment/Tiles/ember_echo_tileset_source_01.png`
- `res://Assets/Environment/Tiles/ember_echo_tileset_64px_01.png`
- `res://Assets/Environment/Tiles/soul_overlay_tiles_64px_01.png`

Godot 使用建议：
- Banana 生成结果先作为源图使用，再裁切、清理成严格网格，最后创建 Godot `TileSet`。
- 当前原型推荐使用 `64x64` tile。先生成稍高质量源图再缩放，通常比直接生成低分辨率图更稳。
- 横版游戏应优先生成侧视平台砖块，不要生成俯视角地板砖。

### Prompt：主 64px 横版 TileSet 图集

目标尺寸：`1024x1024`，透明背景，严格 `4x4` 图集，每格按 `64x64` tile 设计。

```text
Use case: stylized-concept
Asset type: 2D side-scrolling game TileSet atlas source art
Primary request: Create a 4 by 4 atlas of 64x64 side-view platformer tiles for Echo Space, a dark fantasy 2D action game with reality world and soul world overlap.
Scene/backdrop: transparent background, no scene, only isolated tile cells.
Subject: 16 modular terrain tiles: flat ground top, ground middle fill, left edge, right edge, inner corner, outer corner, thin platform, broken platform, cracked wall block, destructible wall face, stone stair step, hanging ledge underside, background stone trim, small rubble decoration, ember-cracked stone variant, cyan soul-cracked stone variant.
Style/medium: stylized painterly game tiles, clean readable silhouettes, side-scroller platform art.
Composition/framing: exact 4x4 grid, each tile centered in its cell, consistent tile scale, no perspective camera, no shadows crossing tile boundaries.
Lighting/mood: low-key dark fantasy, subtle ember orange cracks and pale cyan soul light accents.
Color palette: charcoal black, desaturated blue-gray stone, muted ember orange, pale cyan.
Materials/textures: cracked stone, worn metal bands, dust, ash, faint soul particles only inside tile boundaries.
Constraints: transparent background, no text, no watermark, no characters, no isometric view, no top-down view, must be suitable for slicing into 64x64 Godot TileSet tiles.
Avoid: full scene illustration, random perspective, thick outer frame, bright cartoon grass, sci-fi panels, busy details that break tiling.
```

### Prompt：可拼接地面条与平台变体

目标尺寸：`2048x512`，透明背景，横向条状图。

```text
Use case: stylized-concept
Asset type: 2D side-scrolling platform tile strip
Primary request: Create a horizontal strip of modular side-view ground and platform tiles for Echo Space.
Scene/backdrop: transparent background.
Subject: repeatable stone platform tops, cracked ledges, underside blocks, left and right end caps, small broken variants, and a destructible wall segment.
Style/medium: dark fantasy 2D game environment texture, clean hand-painted look.
Composition/framing: horizontal tile strip arranged left to right, each module aligned to a 64x64 grid, tile-safe spacing and no overlapping shadows.
Lighting/mood: restrained ember glow from cracks, cold cyan soul glow in selected cracks.
Color palette: dark blue-gray stone, charcoal, muted orange, pale cyan.
Materials/textures: chipped stone, dust, ash, thin worn metal braces, subtle cracks.
Constraints: transparent background, no characters, no text, no watermark, side-view only, modular and sliceable for Godot TileMap.
Avoid: top-down floor, realistic photo texture, grassland tiles, excessive ornament, perspective depth.
```

### Prompt：灵魂世界叠加 Tile

目标尺寸：`1024x512`，透明背景。

```text
Use case: stylized-concept
Asset type: overlay TileMap decoration atlas
Primary request: Create modular overlay decoration tiles that make existing reality-world platforms look like they are partially shifted into the soul world.
Scene/backdrop: transparent background.
Subject: cyan soul cracks, faint transparent haze edges, ghostly rim lines, floating shard clusters, small spectral dust patches, broken reality-to-soul transition seams.
Style/medium: stylized dark fantasy overlay sprites for a 2D side-scroller.
Composition/framing: arranged as small tileable overlay pieces, each piece fits within a 64x64 or 128x64 cell, no full background.
Lighting/mood: cold pale cyan, quiet supernatural tension.
Color palette: transparent cyan, pale blue-white, very dark desaturated blue.
Materials/textures: spirit haze, fractured light, fine dust, translucent shard edges.
Constraints: transparent background, no text, no watermark, no solid opaque background, designed to layer above stone tiles in Godot.
Avoid: neon cyberpunk holograms, huge glow bloom, character silhouettes, UI symbols.
```

## 2. UI 补全包

生成后的推荐保存路径：
- `res://Assets/UI/HUD/enemy_health_bar_01.png`
- `res://Assets/UI/HUD/enemy_posture_bar_01.png`
- `res://Assets/UI/HUD/formal_hud_skin_01.png`
- `res://Assets/UI/Inventory/inventory_panel_01.png`
- `res://Assets/UI/Inventory/item_slot_01.png`
- `res://Assets/UI/Inventory/item_slot_selected_01.png`
- `res://Assets/UI/Progression/progression_panel_01.png`
- `res://Assets/UI/Progression/progression_attribute_row_01.png`
- `res://Assets/UI/Progression/progression_point_buttons_01.png`
- `res://Assets/UI/Progression/attribute_icons_01.png`
- `res://Assets/UI/MainMenu/button_pressed_01.png`

### Prompt：敌人血条

目标尺寸：`512x96`，透明背景。

```text
Use case: ui-mockup
Asset type: enemy HUD health bar skin
Primary request: Design a compact enemy health bar frame for a dark fantasy 2D action game.
Scene/backdrop: transparent background.
Subject: a narrow horizontal enemy HP bar frame with a dark worn-metal frame and muted red fill style reference.
Style/medium: polished game HUD asset, serious and readable.
Composition/framing: centered horizontal bar, small enough for above-enemy display, clean middle area for dynamic fill.
Lighting/mood: tense, sharp, restrained.
Color palette: charcoal frame, muted blood red, tiny ember highlights.
Materials/textures: worn metal, chipped lacquer, fine scratches.
Constraints: transparent background, no text, no watermark, no numbers, no enemy portrait.
Avoid: huge boss UI, mobile RPG ornament, glossy plastic, bright arcade red.
```

### Prompt：敌人架势条

目标尺寸：`512x96`，透明背景。

```text
Use case: ui-mockup
Asset type: enemy posture bar skin
Primary request: Design an enemy posture / stagger bar frame inspired by precise parry combat, matching Echo Space's ember and soul visual direction.
Scene/backdrop: transparent background.
Subject: a narrow horizontal posture bar frame with a pressure-building visual language, suitable for display near enemy health.
Style/medium: dark fantasy combat HUD asset.
Composition/framing: centered horizontal bar, slightly sharper and more angular than the health bar, clean interior for dynamic fill.
Lighting/mood: dangerous, focused, close to breaking.
Color palette: dark steel blue, ember orange pressure line, pale cyan hairline accent.
Materials/textures: etched metal, cracked enamel, tiny sparks along the frame.
Constraints: transparent background, no text, no watermark, no portrait, readable at small scale.
Avoid: futuristic loading bar, bright yellow cartoon meter, overdecorated fantasy frame.
```

### Prompt：正式 HUD 皮肤

目标尺寸：`1024x256`，透明背景。

```text
Use case: ui-mockup
Asset type: formal HUD skin atlas
Primary request: Create a compact HUD skin atlas for Echo Space, including frames for player HP, stamina, world indicator, and small status panels.
Scene/backdrop: transparent background.
Subject: multiple dark fantasy HUD frame elements arranged in a clean atlas: long player bar frame, shorter stamina bar frame, compact world indicator frame, small label plate, subtle divider pieces.
Style/medium: polished 2D game HUD asset atlas.
Composition/framing: organized atlas layout, each element separated with clear padding, no text embedded.
Lighting/mood: serious, readable, minimal glow.
Color palette: charcoal, dark steel blue, ember orange, pale cyan.
Materials/textures: worn metal, stone enamel, ash dust, fine engraved seams.
Constraints: transparent background, no text, no watermark, no character portraits, designed to be sliced in Godot.
Avoid: decorative clutter, fantasy MMO gold frame, sci-fi hologram panels, playful mobile UI.
```

### Prompt：背包界面面板

目标尺寸：`1024x1024`，透明背景。

```text
Use case: ui-mockup
Asset type: inventory panel background
Primary request: Design a dark fantasy inventory panel background for Echo Space.
Scene/backdrop: transparent background.
Subject: a large rectangular inventory panel with worn frame, subtle inner surface, side margins for item categories, and clean center area for item slots.
Style/medium: polished game UI panel.
Composition/framing: front-facing rectangular panel, symmetrical enough for UI, readable empty center, no text.
Lighting/mood: quiet, grounded, ancient.
Color palette: dark blue-gray, charcoal, muted ember edge, faint cyan soul seam.
Materials/textures: worn lacquer, brushed metal, cracked stone trim, ash dust.
Constraints: transparent background, no text, no watermark, no item icons, no mockup cursor.
Avoid: backpack photo, parchment fantasy scroll, bright golden MMO inventory, cartoon panels.
```

### Prompt：物品栏格子与选中框

目标尺寸：`1024x512`，透明背景，两行图集。

```text
Use case: ui-mockup
Asset type: inventory slot atlas
Primary request: Create an atlas of item slot frames for Echo Space, including normal, hover, selected, disabled, and empty states.
Scene/backdrop: transparent background.
Subject: square inventory slot frames with dark fantasy styling; selected state uses a restrained pale cyan and ember double-world glow.
Style/medium: polished 2D game UI asset atlas.
Composition/framing: clean grid of square slots, each slot fits a 128x128 cell with padding, no text or icons.
Lighting/mood: functional, crisp, restrained.
Color palette: charcoal, dark steel blue, ember orange, pale cyan.
Materials/textures: worn metal rim, dark enamel center, subtle scratches.
Constraints: transparent background, no item icons, no text, no watermark, easy to slice for Godot.
Avoid: bright mobile inventory squares, gem-like frames, excessive glow, irregular sizes.
```

### Prompt：加点界面面板

目标尺寸：`1024x1024`，透明背景。

使用重点：
- 这张只生成“面板背景”和布局区域，不要让模型把 `+ / -`、文字、数值、进度条直接画死在背景里。
- Godot 里应该用真实 `Button`、`Label`、`ProgressBar`、`TextureRect` 叠上去，这样才能绑定加点、退点、重置等逻辑。
- 如果 Banana 仍然画出按钮或英文文字，这张图建议废弃重来，不要强行接入。

```text
Use case: ui-mockup
Asset type: progression / attribute panel background only
Primary request: Design only the empty background panel for a character progression screen in Echo Space. It must be a reusable UI container, not a complete interface screenshot.
Scene/backdrop: transparent background.
Subject: a serious rectangular UI panel with clean empty zones for a title, unspent point label, five attribute rows, and a description area. Include only subtle empty sockets or anchor spaces where real Godot buttons can be placed later. Do not draw plus buttons, minus buttons, sliders, progress bars, text, numbers, icons, or labels into the panel.
Style/medium: dark fantasy game UI panel.
Composition/framing: front-facing panel, strong header area, five evenly spaced blank attribute row zones, a right-side blank description zone, and clean margins for overlaying real UI controls in Godot.
Lighting/mood: focused, ritual-like, restrained.
Color palette: dark blue-gray, charcoal, ember edge highlights, faint cyan cracks.
Materials/textures: etched metal, stone trim, worn lacquer, subtle spirit seams.
Constraints: transparent background, no embedded words, no numbers, no plus or minus symbols, no circular button icons, no fake bars, no item icons, no watermark, slice-friendly, suitable for real Godot Controls to be layered on top.
Avoid: complete UI screenshot, baked-in buttons, baked-in text, baked-in sliders, modern app dashboard, parchment scroll, bright fantasy gold, sci-fi control panel.
```

### Prompt：加点属性行底板

目标尺寸：`1024x256`，透明背景，建议生成成一组可切片行底板。

使用重点：
- 这是每一条属性的横向底板，不承载真实文字和按钮。
- Godot 中每一行建议用 `HBoxContainer` 组织：属性图标、属性名、数值、减号按钮、加号按钮。

```text
Use case: ui-mockup
Asset type: progression attribute row plate atlas
Primary request: Create reusable row plates for a character progression attribute list in Echo Space.
Scene/backdrop: transparent background.
Subject: a small atlas of horizontal attribute row background plates, each row has an icon socket on the left, a clean center area for a label and value, and reserved empty space on the right where real Godot plus and minus buttons will be placed. Do not draw the plus or minus buttons.
Style/medium: dark fantasy game UI component, clean and functional.
Composition/framing: 4 to 6 horizontal row plates arranged in a grid, consistent height, clear padding, front-facing.
Lighting/mood: focused, readable, restrained.
Color palette: dark steel blue, charcoal, subtle ember edge, faint cyan seam.
Materials/textures: worn metal, dark enamel, fine scratches, subtle spirit cracks.
Constraints: transparent background, no text, no numbers, no plus or minus symbols, no progress bars, no watermark, easy to slice and repeat in Godot.
Avoid: complete interface screen, embedded controls, decorative clutter, mobile RPG achievement style.
```

### Prompt：加点加减按钮图集

目标尺寸：`1024x512`，透明背景，按钮图集。

使用重点：
- 这张专门生成真正可以映射到 Godot `Button` 的 `+ / -` 按钮贴图。
- 建议至少包含 normal、hover、pressed、disabled 四态，`+` 和 `-` 分开成两组。

```text
Use case: ui-mockup
Asset type: progression plus and minus button state atlas
Primary request: Create a transparent atlas of small plus and minus buttons for Echo Space's character progression screen.
Scene/backdrop: transparent background.
Subject: plus and minus round or compact square buttons in four states: normal, hover, pressed, and disabled. Each button should be isolated in its own cell with consistent size and padding.
Style/medium: polished dark fantasy UI button asset.
Composition/framing: clean grid atlas, two rows for plus and minus, four columns for normal, hover, pressed, disabled. Each cell must be easy to slice into a Godot Button texture.
Lighting/mood: tactile, serious, responsive.
Color palette: dark steel blue base, muted ember edge for active, pale cyan highlight for hover, desaturated gray for disabled.
Materials/textures: worn metal rim, dark enamel center, subtle engraved symbol.
Text (verbatim): "+" and "-"
Constraints: transparent background, no extra text, no numbers, no labels, no watermark, consistent button size, symbol must be centered and readable.
Avoid: baked-in full panel, random loose icons, mobile candy button, gold fantasy ornament, overly complex tiny symbols.
```

### Prompt：属性图标与说明面板

目标尺寸：`1024x1024`，透明背景。

```text
Use case: ui-mockup
Asset type: attribute icon atlas and tooltip plate
Primary request: Create a small icon atlas for player attributes in Echo Space, plus one matching description plate.
Scene/backdrop: transparent background.
Subject: attribute icons for vitality, endurance, attack power, deflect precision, inventory utility, and soul resonance; include one empty description plate.
Style/medium: readable 2D game UI icons, dark fantasy, symbolic not cartoon.
Composition/framing: organized icon atlas, each icon fits a 128x128 cell, consistent silhouette and frame style.
Lighting/mood: crisp, mystical, serious.
Color palette: bone white symbol lines, ember orange and pale cyan accents, dark frame.
Materials/textures: etched metal medallions, subtle glow, worn edges.
Constraints: transparent background, no text, no watermark, icons must remain readable at small size.
Avoid: emoji style, mobile achievement badges, complex tiny illustrations, bright rainbow icon set.
```

### Prompt：按钮按下状态

目标尺寸：`640x160`，透明背景。

```text
Use case: ui-mockup
Asset type: main menu button pressed state
Primary request: Create the pressed/down state for an Echo Space dark fantasy main menu button, matching the existing idle and hover direction.
Scene/backdrop: transparent background.
Subject: a single horizontal button plate, visually depressed inward with darker center and stronger bottom shadow.
Style/medium: polished game UI button asset.
Composition/framing: wide centered plate, clean center for Chinese menu text.
Lighting/mood: tactile, responsive, serious.
Color palette: dark steel blue base, compressed ember line, faint cyan edge retained.
Materials/textures: worn metal, cracked lacquer, subtle engraved seams.
Constraints: transparent background, no text, no watermark, same general shape family as the idle and hover button states.
Avoid: huge glow, plastic shine, rounded mobile button, gold ornament overload.
```

## 3. 特效补充贴图 Prompt

生成后的推荐保存路径：
- `res://Assets/VFX/Combat/hit_impact_spritesheet_01.png`
- `res://Assets/VFX/Combat/deflect_spark_atlas_01.png`
- `res://Assets/VFX/Combat/blade_arc_atlas_01.png`
- `res://Assets/VFX/Combat/posture_break_ring_01.png`
- `res://Assets/VFX/Combat/execution_prompt_glyph_01.png`
- `res://Assets/VFX/Combat/execution_impact_spritesheet_01.png`
- `res://Assets/VFX/Combat/hit_feedback_particles_01.png`
- `res://Assets/VFX/World/world_switch_overlay_atlas_01.png`

Godot 使用建议：
- 这些资源应作为特效补充贴图使用，不要把它们当作完整玩法逻辑。
- 推荐节点：spritesheet 用 `AnimatedSprite2D`；一次性闪光用 `Sprite2D + AnimationPlayer`；火花和碎片用 `GPUParticles2D`；闪白、描边和受击反馈用 `CanvasItem` shader；UI 呼吸、缩放和提示动效用 `Tween`。

### Prompt：普通攻击命中 spritesheet

目标尺寸：`1024x256`，透明背景，`8` 帧横向一行。

```text
Use case: stylized-concept
Asset type: transparent combat hit impact spritesheet
Primary request: Create an 8-frame one-row spritesheet for a normal melee hit impact in Echo Space.
Scene/backdrop: transparent background.
Subject: a compact slash impact burst with ash, tiny stone chips, muted ember sparks, and a faint cyan echo afterimage.
Style/medium: stylized 2D combat VFX, hand-painted game effect.
Composition/framing: 8 equal frames left to right, centered impact, no camera background, frame-safe spacing.
Lighting/mood: quick, sharp, physical, not magical overload.
Color palette: off-white impact core, muted ember orange, dark smoke gray, faint pale cyan echo.
Materials/textures: slash streaks, sparks, dust, small debris.
Constraints: transparent background, no text, no watermark, no weapon or character, designed for AnimatedSprite2D.
Avoid: giant explosion, blood gore, full-scene illustration, excessive bloom, anime screen-filling effects.
```

### Prompt：弹反火花图集

目标尺寸：`1024x512`，透明背景。

```text
Use case: stylized-concept
Asset type: parry deflect spark atlas
Primary request: Create a transparent atlas of deflect and parry sparks for a precise melee combat game.
Scene/backdrop: transparent background.
Subject: multiple spark clusters, sharp crossing impact stars, tiny ember fragments, short cyan shock glints, and thin metal-spark streaks.
Style/medium: stylized 2D combat particle texture atlas.
Composition/framing: separated small VFX elements arranged in a clean grid, no overlapping elements, each usable as a particle texture.
Lighting/mood: crisp, metallic, high-impact.
Color palette: hot ember orange, pale yellow-white core, pale cyan echo edge.
Materials/textures: metal sparks, small shards, impact starbursts.
Constraints: transparent background, no text, no watermark, no sword, no character, suitable for GPUParticles2D and one-shot sprite bursts.
Avoid: fireworks, magic circles, neon sci-fi lasers, huge explosion clouds.
```

### Prompt：刀光弧线图集

目标尺寸：`1024x512`，透明背景。

```text
Use case: stylized-concept
Asset type: melee blade arc texture atlas
Primary request: Create a transparent atlas of stylized melee blade arcs for Echo Space.
Scene/backdrop: transparent background.
Subject: several crescent slash arcs in different lengths, with off-white sharp cores, muted ember trailing edge, and faint cyan afterimage.
Style/medium: 2D action game slash VFX, clean and slice-friendly.
Composition/framing: each slash arc isolated with clear padding, no character or weapon attached, multiple arc sizes in a grid.
Lighting/mood: fast, elegant, dangerous.
Color palette: off-white, ember orange, pale cyan, transparent fade.
Materials/textures: soft motion smear, sharp cutting edge, subtle dust particles.
Constraints: transparent background, no text, no watermark, no full body, no background.
Avoid: giant anime beam, sci-fi laser blade, saturated purple, messy overlapping strokes.
```

### Prompt：架势打满 / 破绽冲击环

目标尺寸：`1024x1024`，透明背景。

```text
Use case: stylized-concept
Asset type: posture break shock ring texture
Primary request: Create circular shock-ring textures for an enemy posture break moment in Echo Space.
Scene/backdrop: transparent background.
Subject: several ring-shaped burst textures, cracked ember pressure lines with pale cyan echo fractures, suitable for scale/fade animation in Godot.
Style/medium: stylized 2D combat VFX texture atlas.
Composition/framing: isolated circular rings and partial rings, centered, clear padding, no full scene.
Lighting/mood: sudden break, pressure release, decisive.
Color palette: ember orange, off-white impact core, pale cyan edge, transparent smoky gray.
Materials/textures: cracked light, dust ring, sharp pressure lines.
Constraints: transparent background, no text, no watermark, no enemy body, designed for Sprite2D + AnimationPlayer scale/fade.
Avoid: magic summoning circle, complex readable runes, sci-fi target reticle, huge explosion.
```

### Prompt：处决提示符号

目标尺寸：`512x512`，透明背景。

```text
Use case: ui-mockup
Asset type: execution available prompt glyph
Primary request: Create a compact execution-available glyph for Echo Space, shown when an enemy posture is broken.
Scene/backdrop: transparent background.
Subject: a sharp symbolic glyph combining a broken stance mark, a decisive cut, and subtle double-world echo.
Style/medium: dark fantasy combat UI/VFX icon.
Composition/framing: centered icon, readable at small size, no words.
Lighting/mood: urgent but restrained, dangerous and clean.
Color palette: bone white core, ember orange warning accent, pale cyan echo rim.
Materials/textures: etched symbol, cracked light, faint ash particles.
Constraints: transparent background, no text, no watermark, no skull, no gore, suitable for pulsing UI animation.
Avoid: button prompt letters, anime face icon, bright arcade warning sign, complex unreadable symbol.
```

### Prompt：处决命中 spritesheet

目标尺寸：`1024x512`，透明背景，`4x2` 帧图集。

```text
Use case: stylized-concept
Asset type: execution impact spritesheet
Primary request: Create an 8-frame execution impact spritesheet for a decisive melee finisher in Echo Space.
Scene/backdrop: transparent background.
Subject: a strong close-range slash burst, compressed shock flash, ember shards, cyan soul rupture, and fading ash.
Style/medium: stylized 2D combat VFX, intense but readable.
Composition/framing: 4 columns by 2 rows, equal frame spacing, centered impact, no character body.
Lighting/mood: decisive, heavy, ritual-like, not gory.
Color palette: off-white flash core, ember orange, pale cyan soul rupture, dark smoke gray.
Materials/textures: slash streaks, cracked light, particle shards, smoke fade.
Constraints: transparent background, no text, no watermark, no gore, no full background, suitable for AnimatedSprite2D.
Avoid: blood explosion, screen-wide anime beam, magical summoning circle, overexposed bloom.
```

### Prompt：受击反馈粒子图集

目标尺寸：`1024x512`，透明背景。

```text
Use case: stylized-concept
Asset type: hit feedback particle atlas
Primary request: Create small particle textures for player and enemy hit feedback in Echo Space.
Scene/backdrop: transparent background.
Subject: small ash puffs, ember specks, cyan soul fragments, tiny cracked-stone chips, short impact streaks.
Style/medium: 2D game particle texture atlas.
Composition/framing: isolated particle elements in a clean grid, each with padding, no big central effect.
Lighting/mood: tactile, restrained, useful for combat feedback.
Color palette: smoky gray, muted ember, pale cyan, off-white highlight.
Materials/textures: dust, sparks, stone chips, soul shards.
Constraints: transparent background, no text, no watermark, no characters, designed for GPUParticles2D.
Avoid: large explosion sprites, blood drops, glitter confetti, sci-fi neon particles.
```

### Prompt：世界切换叠加特效图集

目标尺寸：`1024x1024`，透明背景。

```text
Use case: stylized-concept
Asset type: world switching overlay VFX atlas
Primary request: Create transparent overlay textures for switching between reality world and soul world in Echo Space.
Scene/backdrop: transparent background.
Subject: ripple arcs, torn-space seams, cyan spirit haze, ember ash streaks, small fractured transition bands.
Style/medium: stylized 2D screen-space VFX texture atlas.
Composition/framing: multiple isolated overlay pieces arranged with padding, no full-screen opaque background.
Lighting/mood: supernatural transition, quiet but dramatic.
Color palette: pale cyan, muted ember, smoky charcoal, transparent fades.
Materials/textures: torn light, haze, ash, spirit particles, fractured seams.
Constraints: transparent background, no text, no watermark, no character silhouettes, designed to be layered and animated in Godot.
Avoid: portal photo, sci-fi hologram grid, huge magic circle, saturated purple cloud.
```
