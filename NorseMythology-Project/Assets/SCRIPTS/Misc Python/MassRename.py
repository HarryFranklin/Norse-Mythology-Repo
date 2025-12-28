import os

def rename_sprites_by_folder(root_path, dry_run=True):
    """
    Renames files from "TREE TYPE - SUFFIX.png" to "TREE TYPE - [FOLDER NAME].png"
    """
    if not os.path.exists(root_path):
        print(f"Error: Path not found -> {root_path}")
        return

    print(f"Scanning: {root_path}")
    if dry_run:
        print("--- DRY RUN MODE (No files will be changed) ---\n")
    else:
        print("--- LIVE MODE (Renaming files...) ---\n")

    count = 0

    for current_root, dirs, files in os.walk(root_path):
        for filename in files:
            if not filename.lower().endswith(".png"):
                continue

            # 1. Get the 'Colour' from the folder name (e.g., "Cold")
            colour_name = os.path.basename(current_root)

            # 2. Get the 'Tree Type' from the filename
            # We split by " - " and take the first chunk.
            # Ex: "Large Spruce - Medium Snow - Spritesheet.png" -> "Large Spruce"
            name_parts = filename.split(" - ")
            
            # Safety check: ensure the file actually has a " - " inside it
            if len(name_parts) > 1:
                tree_type = name_parts[0]
                
                # 3. Construct the new name
                new_filename = f"{tree_type} - {colour_name}.png"
                
                # Skip if name is already correct
                if filename == new_filename:
                    continue

                old_file_path = os.path.join(current_root, filename)
                new_file_path = os.path.join(current_root, new_filename)

                # 4. Rename
                try:
                    if dry_run:
                        print(f"[PREVIEW] Rename: '{filename}'\n       To: '{new_filename}'")
                    else:
                        os.rename(old_file_path, new_file_path)
                        print(f"[RENAMED] {new_filename}")
                    
                    count += 1
                except Exception as e:
                    print(f"[ERROR] Could not rename {filename}: {e}")

    print(f"\nOperation complete. Processed {count} files.")

# ==========================================
# SETTINGS
# ==========================================

# 1. PASTE YOUR FULL PATH HERE
target_path = r"E:\Game Dev\Norse-Mythology-Repo\NorseMythology-Project\Assets\Art\Sprites\Pixel Art Spruce Tree Pack - Snow Edition"

# 2. SET THIS TO 'False' TO ACTUALLY RENAME THE FILES
# Keep it True first to check the output!
rename_sprites_by_folder(target_path, dry_run=False)