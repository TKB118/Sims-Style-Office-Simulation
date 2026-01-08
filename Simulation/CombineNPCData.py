import os
import pandas as pd
import glob

def combine_csvs():
    # Define directories
    base_path = r"c:\Users\PC_user\Desktop\OfficeNPCSimulation - コピー\Assets"
    dirs_to_search = ["NPC Worker Data", "NPC Worker Data 2"]
    
    all_files = []
    for d in dirs_to_search:
        path = os.path.join(base_path, d)
        if os.path.exists(path):
            files = glob.glob(os.path.join(path, "*.csv"))
            all_files.extend(files)
            print(f"Found {len(files)} CSVs in {d}")
        else:
            print(f"Directory not found: {path}")

    if not all_files:
        print("No CSV files found.")
        return

    combined_df = pd.DataFrame()
    
    for f in all_files:
        try:
            # Read CSV
            df = pd.read_csv(f)
            
            # Add SourceFile column (using filename only)
            df['SourceFile'] = os.path.basename(f)
            
            # Append to combined DataFrame
            combined_df = pd.concat([combined_df, df], ignore_index=True)
        except Exception as e:
            print(f"Error reading {f}: {e}")

    # Write to Excel
    output_path = os.path.join(base_path, "CombinedNPCData.xlsx")
    try:
        combined_df.to_excel(output_path, index=False)
        print(f"Successfully created {output_path} with {len(combined_df)} rows.")
    except Exception as e:
        print(f"Error writing to Excel: {e}")

if __name__ == "__main__":
    try:
        import pandas
        import openpyxl
        combine_csvs()
    except ImportError as e:
        print(f"Missing dependency: {e}. Please install pandas and openpyxl.")
