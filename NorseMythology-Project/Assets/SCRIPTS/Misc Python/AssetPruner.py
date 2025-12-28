import os
import shutil

def delete_specific_folders(root_path):
    if not os.path.exists(root_path):
        print(f"Error: Path not found -> {root_path}")
        return

    print(f"Starting cleanup in: {root_path}")
    deleted_count = 0

    # Walk through the directory tree
    # topdown=True is required so we can modify 'dirs' in-place
    for current_root, dirs, files in os.walk(root_path, topdown=True):
        
        # We iterate over a copy (list(dirs)) so we can remove items from 'dirs' safely
        for dirname in list(dirs):
            
            # Normalize name to lowercase and strip whitespace for comparison
            name_check = dirname.lower().strip()
            
            should_delete = False
            
            # 1. Check for "Snow - High" (Matches "Snow - High", "snow - high", etc.)
            if "snow" in name_check and "high" in name_check:
                should_delete = True
                
            # 2. Check for "Yellow" (Matches "Yellow", "yellow", "YELLOW")
            elif name_check == "yellow":
                should_delete = True

            if should_delete:
                full_path = os.path.join(current_root, dirname)
                try:
                    print(f"Deleting: {full_path}")
                    shutil.rmtree(full_path) # Deletes folder AND contents
                    
                    # Remove from 'dirs' list so os.walk doesn't try to go inside it
                    dirs.remove(dirname)
                    deleted_count += 1
                except Exception as e:
                    print(f"FAILED to delete {full_path}. Reason: {e}")

    print(f"\nCleanup Complete! Deleted {deleted_count} folders.")

target_folder = r"C:\Users\hrfra\Downloads\Pixel Art Spruce Tree Pack - Snow Edition\Pixel Art Spruce Tree Pack - Snow Edition"

delete_specific_folders(target_folder)