#!/usr/bin/env python3
"""
Grid CSV Generator for Fire Simulation

This script generates CSV grid files filled with zeros for use with the MARS Fire simulation.
The generated files serve as base grids that will be overwritten by the FireLayer's 
density-based forest generation algorithm.

Usage:
    python grid_generator.py

The script will prompt for grid dimensions and automatically create the CSV file
in the correct format for the MARS framework.

Author: Dominik Wiesendanger
Date: 2025-08-02
"""

import csv
import os
import sys


def create_grid_csv(width, height, filename):
    """
    Creates a CSV grid file filled with zeros.
    
    Args:
        width (int): Number of columns in the grid
        height (int): Number of rows in the grid
        filename (str): Name of the output CSV file
        
    Returns:
        bool: True if file was created successfully, False otherwise
    """
    try:
        # Ensure the filename has .csv extension
        if not filename.endswith('.csv'):
            filename += '.csv'
        
        # Create the grid data (all zeros)
        grid_data = []
        for row in range(height):
            # Create a row with 'width' number of zeros
            row_data = ['0'] * width
            grid_data.append(row_data)
        
        # Write to CSV file using semicolon delimiter (MARS format)
        with open(filename, 'w', newline='', encoding='utf-8') as csvfile:
            writer = csv.writer(csvfile, delimiter=';')
            
            # Write all rows to the file
            for row in grid_data:
                writer.writerow(row)
        
        print(f"Successfully created {filename} ({width}x{height} grid)")
        print(f"File size: {os.path.getsize(filename)} bytes")
        return True
        
    except Exception as e:
        print(f"Error creating {filename}: {e}")
        return False


def get_user_input():
    """
    Gets grid dimensions and filename from user input with validation.
    
    Returns:
        tuple: (width, height, filename) or (None, None, None) if cancelled
    """
    try:
        print("Fire Simulation - Grid CSV Generator")
        print("=" * 50)
        print("This tool creates empty grid files for the Fire simulation.")
        print("All cells will be filled with zeros (empty) - the FireLayer will")
        print("generate trees based on the density parameter in config.json.\n")
        
        # Get width
        while True:
            try:
                width = int(input("Enter grid width (columns): "))
                if width <= 0:
                    print("Width must be greater than 0")
                    continue
                if width > 1000:
                    confirm = input(f"Large grid ({width} columns). Continue? (y/n): ")
                    if confirm.lower() != 'y':
                        continue
                break
            except ValueError:
                print("Please enter a valid number")
        
        # Get height
        while True:
            try:
                height = int(input("Enter grid height (rows): "))
                if height <= 0:
                    print("Height must be greater than 0")
                    continue
                if height > 1000:
                    confirm = input(f"Large grid ({height} rows). Continue? (y/n): ")
                    if confirm.lower() != 'y':
                        continue
                break
            except ValueError:
                print("Please enter a valid number")
        
        # Get filename
        default_filename = f"grid_{width}x{height}.csv"
        print(f"\nSuggested filename: {default_filename}")
        filename = input("Enter filename (or press Enter for default): ").strip()
        
        if not filename:
            filename = default_filename
        
        # Show summary
        total_cells = width * height
        estimated_size = total_cells * 2  # Rough estimate: each cell = "0;"
        
        print(f"\nGrid Summary:")
        print(f"   Dimensions: {width} x {height}")
        print(f"   Total cells: {total_cells:,}")
        print(f"   Estimated file size: ~{estimated_size:,} bytes")
        print(f"   Output file: {filename}")
        
        confirm = input("\nCreate this grid file? (y/n): ")
        if confirm.lower() != 'y':
            print("Operation cancelled")
            return None, None, None
            
        return width, height, filename
        
    except KeyboardInterrupt:
        print("\nOperation cancelled by user")
        return None, None, None


def main():
    """
    Main function that handles user interaction and grid creation.
    """
    try:
        # Get user input for custom grid creation
        width, height, filename = get_user_input()
        if width is not None:
            create_grid_csv(width, height, filename)
        else:
            print("Goodbye!")
            
    except KeyboardInterrupt:
        print("\nGoodbye!")
    except Exception as e:
        print(f"Unexpected error: {e}")


if __name__ == "__main__":
    main()
