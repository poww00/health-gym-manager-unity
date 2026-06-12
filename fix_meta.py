import re
import uuid
import random

meta_path = r'D:\unity\My project\Assets\_Project\Resources\GeneratedRuntimeUI\characters\customer\body\male_chubby\body_male_chubby_walk_front_32x48_4x2.png.meta'
with open(meta_path, 'r', encoding='utf-8') as f:
    meta_content = f.read()

sprites_block = "    sprites:\n"
name_table_block = "    nameFileIdTable:\n"

for i in range(8):
    row = i // 4
    col = i % 4
    x = col * 32
    y = 48 if row == 0 else 0  # Frame 0-3 at top (y=48), 4-7 at bottom (y=0)

    sprite_name = f"body_male_chubby_walk_front_32x48_4x2_{i}"
    sprite_id = uuid.uuid4().hex
    internal_id = random.randint(-9223372036854775808, 9223372036854775807)
    
    sprites_block += f"""    - serializedVersion: 2
      name: {sprite_name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: 32
        height: 48
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: {internal_id}
      vertices: []
      indices: 
      edges: []
      weights: []
"""
    name_table_block += f"      {sprite_name}: {internal_id}\n"

meta_content = re.sub(r'    sprites:.*?(?=    outline:)', sprites_block, meta_content, flags=re.DOTALL)
meta_content = re.sub(r'    nameFileIdTable:.*?(?=  mipmapLimitGroupName:)', name_table_block, meta_content, flags=re.DOTALL)

with open(meta_path, 'w', encoding='utf-8') as f:
    f.write(meta_content)

print("Meta file updated with 8 slices!")
