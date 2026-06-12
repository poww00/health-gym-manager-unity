from PIL import Image

def process():
    in_path = r'D:\unity\My project\Assets\_Project\Resources\GeneratedRuntimeUI\characters\customer\body\male_chubby\body_male_chubby_walk_front_32x48_4x2.png'
    img = Image.open(in_path).convert('RGBA')
    
    # 1. Resize to 128x96 using NEAREST to preserve colors and pixels
    img = img.resize((128, 96), Image.NEAREST)
    
    width, height = img.size
    pixels = img.load()
    
    # 2. Flood fill to remove background
    # We will use a simple BFS queue.
    # The background is white (r>240, g>240, b>240)
    visited = set()
    queue = [(0, 0)]
    
    # Add all edge pixels to queue if they are white
    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))
        
    while queue:
        x, y = queue.pop(0)
        if (x, y) in visited:
            continue
        if x < 0 or x >= width or y < 0 or y >= height:
            continue
        
        visited.add((x, y))
        r, g, b, a = pixels[x, y]
        
        if r > 240 and g > 240 and b > 240 and a > 0:
            pixels[x, y] = (0, 0, 0, 0)
            queue.append((x+1, y))
            queue.append((x-1, y))
            queue.append((x, y+1))
            queue.append((x, y-1))

    # 3. Remove neck stump for each 32x48 frame
    for frame_idx in range(8):
        fx = (frame_idx % 4) * 32
        fy = (frame_idx // 4) * 48
        
        for y in range(48):
            min_x, max_x = 32, -1
            for x in range(32):
                r, g, b, a = pixels[fx+x, fy+y]
                if a > 0: # Non-transparent
                    if x < min_x: min_x = x
                    if x > max_x: max_x = x
            
            w = max_x - min_x + 1 if max_x >= min_x else 0
            
            if w > 0:
                if w <= 10:
                    # This is the neck stump! Remove it.
                    for x in range(32):
                        pixels[fx+x, fy+y] = (0, 0, 0, 0)
                else:
                    # Hit the shoulders!
                    break

    # Save exactly over the target file
    img.save(in_path)
    print(f'Successfully processed and saved to {in_path}')

if __name__ == '__main__':
    process()
