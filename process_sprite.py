import os
from PIL import Image

def remove_background_and_neck():
    in_path = r'D:\unity\My project\Assets\_Project\Resources\GeneratedRuntimeUI\characters\customer\body\male_chubby\body_male_chubby_walk_front_32x48_4x2.png'
    
    img = Image.open(in_path).convert('RGBA')
    
    # 1. Downscale to 128x96 using NEAREST as per '어떤 픽셀도 보정하지 말 것'
    img = img.resize((128, 96), Image.NEAREST)
    
    pixels = img.load()
    width, height = img.size
    
    # 2. Identify the white background color. Usually it's perfectly white (255, 255, 255)
    # The tank top might be slightly off-white, or perfectly white.
    # If the background is exactly white in the top-left corner:
    bg_color = pixels[0, 0]
    
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            
            # Remove background. We use a threshold to be safe, but wait, tank top is white!
            # It's better to do a flood fill from the corners to remove the background 
            # if the tank top is enclosed. Or just remove exact white, and assume tank top has some grey/shading.
            if (r, g, b) == (255, 255, 255) or (r > 250 and g > 250 and b > 250):
                # Let's check if this is likely background by checking if it's near the edges.
                # Actually, let's just make it completely transparent if it's exactly white or near white.
                pass
                
    # To be safe and preserve the tank top perfectly, I will first just save it as is and analyze it.
    img.save('test_downscale.png')
    print("Saved test_downscale.png")

if __name__ == '__main__':
    remove_background_and_neck()
