# send_whatsapp_message.py
import sys
import urllib.parse
import time
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import os
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

chromedriver_path = r"<PATH TO YOUR CHROME DRIVER>" #C:\Windows\chromedriver.exe
user_data_dir = r"C:\Users\<YOUR USERNAME>\AppData\Local\Google\Chrome\User Data\AutomationProfile"

def launch_driver(headless=True):
    chrome_options = Options()
    chrome_options.add_argument(f"--user-data-dir={user_data_dir}")
    chrome_options.add_argument("--disable-extensions")
    chrome_options.add_argument("--no-sandbox")
    chrome_options.add_argument("--disable-dev-shm-usage")
    chrome_options.add_argument("--disable-popup-blocking")
    if headless:
        chrome_options.add_argument("--headless=new")
    return webdriver.Chrome(service=Service(chromedriver_path), options=chrome_options)

def check_login_status(driver):
    try:
        WebDriverWait(driver, 20).until(
            EC.any_of(
                EC.presence_of_element_located((By.XPATH, '//canvas[@aria-label="Scan this QR code to link a device!"]')),
                EC.presence_of_element_located((By.XPATH, '//div[@aria-label="Chat list"]'))
            )
        )

        if driver.find_elements(By.XPATH, '//canvas[@aria-label="Scan this QR code to link a device!"]'):
            print(" User is NOT logged in (QR code found).")
            return "qr_code"
        elif driver.find_elements(By.XPATH, '//div[@aria-label="Chat list"]'):
            print(" User is logged in (Chat list found).")
            return "logged_in"
        else:
            print(" Could not detect login state.")
            return "unknown"

    except Exception as e:
        print(f"An error occurred during login detection: {e}")
        return "unknown"

def send_message(driver, number, receiver_name, message, file_path):
    try:
        encoded_number = urllib.parse.quote(number)
        chat_url = f"https://web.whatsapp.com/send?phone={encoded_number}"
        print(f"Opening chat with {receiver_name}...")
        driver.get(chat_url)

        WebDriverWait(driver, 20).until(
            EC.presence_of_element_located((By.XPATH, '//div[@aria-label="Type a message"]'))
        )

        # Send text message if provided
        if message:
            print(f"Sending message to {receiver_name}...")
            message_box = driver.find_element(By.XPATH, '//div[@aria-label="Type a message"]')
            message_box.send_keys(message)
            time.sleep(2)

            send_button = driver.find_element(By.XPATH, '//button[@aria-label="Send"]')
            send_button.click()
            time.sleep(2)
            print(f" Message sent successfully to {receiver_name}!")

        # Send file if provided
        if file_path and os.path.exists(file_path):
            print(f"Sending file '{file_path}' to {receiver_name}...")
            # attach_button = driver.find_element(By.XPATH, '//div[@title="Attach"]')
            attach_button = WebDriverWait(driver, 20).until(
                EC.element_to_be_clickable((By.XPATH, '//button[@title="Attach"]'))
            )
            attach_button.click()
            time.sleep(1)

            # Locate the file input field
            file_input = driver.find_element(By.XPATH, '//input[@accept="*"]')
            file_input.send_keys(file_path)
            time.sleep(2)

            # Click the send button for the attachment
            send_attachment_button = WebDriverWait(driver, 15).until(
            EC.element_to_be_clickable((By.XPATH, '//div[@aria-label="Send"]'))
        )
            send_attachment_button.click()
            time.sleep(3)
            print(f" File sent successfully to {receiver_name}!")

    except Exception as e:
        print(f"Failed to send message/file to {receiver_name}: {e}")
        sys.exit(2)  # Error


def send_whatsapp_message(number, receiver_name, message, file_path):
    if message or (file_path and os.path.exists(file_path)):
        needs_headless = True
        # if file_path and os.path.exists(file_path):
        #     needs_headless = False  # Needs GUI for file upload

        driver = launch_driver(headless=needs_headless)
        driver.get("https://web.whatsapp.com")

        login_status = check_login_status(driver)
        driver.quit()

        if login_status == "qr_code":
            print("User is NOT logged in. Relaunching in non-headless mode for QR scan.")
            driver = launch_driver(headless=False)
            driver.get("https://web.whatsapp.com")
            print(" Please scan the QR code in the browser window to log in.")
            try:
                WebDriverWait(driver, 300).until(
                    EC.presence_of_element_located((By.XPATH, '//div[@aria-label="Chat list"]'))
                )
                print(" Successfully logged in! Please rerun the script to send messages.")
                driver.quit()
                sys.exit(1)
            except Exception as e:
                print(f" User did not scan QR code in time: {e}")
                driver.quit()
                sys.exit(2)
        elif login_status == "logged_in":
            print(" User is already logged in. Proceeding to send message or file.")
            driver = launch_driver(headless=needs_headless)
            driver.get("https://web.whatsapp.com")
            send_message(driver, number, receiver_name, message, file_path)
            driver.quit()
            print(" Script completed.")
            sys.exit(0)
        else:
            print(" Could not determine login state. Exiting.")
            sys.exit(2)
    else:
        print("✅ No message or file to send. Exiting successfully.")
        sys.exit(0)


# MAIN ENTRY POINT
if __name__ == "__main__":
    if len(sys.argv) != 5:
        print("Usage: python send_whatsapp_message.py <number> <receiver_name> <message> <file_path>")
        sys.exit(2)

    recipient_number = sys.argv[1]
    receiver_name = sys.argv[2]
    message_text = sys.argv[3]
    file_path = sys.argv[4]

    send_whatsapp_message(recipient_number, receiver_name, message_text, file_path)
