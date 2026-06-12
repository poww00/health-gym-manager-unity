import sys
from PIL import Image

def process_idle(input_path, output_path):
    # Open the image and convert to RGBA
    img = Image.open(input_path).convert("RGBA")
    data = img.getdata()
    
    # Define white threshold
    new_data = []
    for item in data:
        # If pixel is white or very close to white, make it transparent
        if item[0] > 240 and item[1] > 240 and item[2] > 240:
            new_data.append((255, 255, 255, 0))
        else:
            new_data.append(item)
            
    img.putdata(new_data)
    
    # The image is 2752x1536, representing a 4x2 grid of frames.
    # The user wants it to be similar in width to the walk sprite.
    # The walk sprite has frames of 32x48.
    # So a 4x2 grid should be 128x96.
    # However, to make it slightly less "thick/wide", we can scale the width down slightly more,
    # e.g., 28x48 per frame -> 112x96 total size.
    target_width = 112
    target_height = 96
    
    img = img.resize((target_width, target_height), Image.Resampling.LANCZOS)
    img.save(output_path, "PNG")
    print(f"Saved processed image to {output_path}")

if __name__ == "__main__":
    process_idle(sys.argv[1], sys.argv[2])
