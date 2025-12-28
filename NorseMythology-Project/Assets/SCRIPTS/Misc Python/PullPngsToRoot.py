import os
import shutil

def move_pngs_to_root():
    # 1. Ask user for the path
    print("--- PNG Organiser ---")
    root_dir = input("Please paste the full path of the Main Folder: ").strip()
    
    # Remove quotes if the user pasted them (common with "Copy as Path")
    root_dir = root_dir.replace('"', '').replace("'", "")

    # 2. Verify the path exists
    if not os.path.isdir(root_dir):
        print(f"\nError: The path '{root_dir}' does not exist or is not a folder.")
        return

    print(f"\nScanning for .png files in: {root_dir}")
    print("-" * 30)

    files_moved = 0
    
    # 3. Walk through the directory tree
    for current_folder, dirs, files in os.walk(root_dir):
        
        # Skip the root folder itself so we don't try to move files that are already there
        if current_folder == root_dir:
            continue

        for filename in files:
            if filename.lower().endswith('.png'):
                # Construct full file paths
                source_path = os.path.join(current_folder, filename)
                destination_path = os.path.join(root_dir, filename)

                # Check if a file with the same name already exists in the root
                if os.path.exists(destination_path):
                    print(f"Duplicate name found: {filename}")
                    
                    # Create a unique name to prevent overwriting
                    base, extension = os.path.splitext(filename)
                    counter = 1
                    while os.path.exists(os.path.join(root_dir, f"{base}_{counter}{extension}")):
                        counter += 1
                    
                    new_filename = f"{base}_{counter}{extension}"
                    destination_path = os.path.join(root_dir, new_filename)
                    print(f"  -> Renaming to: {new_filename}")

                # Move the file
                try:
                    shutil.move(source_path, destination_path)
                    print(f"Moved: {filename}")
                    files_moved += 1
                except Exception as e:
                    print(f"Error moving {filename}: {e}")

    print("-" * 30)
    print(f"Process complete. Total files moved: {files_moved}")
    input("Press Enter to close...")

if __name__ == "__main__":
    move_pngs_to_root()