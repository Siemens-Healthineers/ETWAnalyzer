#include <iostream>
#include <thread>
#include <string>
#include <windows.h>
#include <chrono>
#include <atomic>
#include <vector>

std::string format_time()
{
    char date[256];
    auto now = std::chrono::system_clock::now();
    auto in_time_t = std::chrono::system_clock::to_time_t(now);

    std::strftime(date, sizeof(date), "%OH:%OM:%OS", std::gmtime(&in_time_t));
    return date;
}

void print(const char* str)
{
    std::cout << format_time() << " " << str << "\n";
}

void formatLine(const char* format, ...)
{
    va_list args;
    va_start(args, format);

    vprintf(format, args);

    va_end(args);
}

class EventManager
{
public:
    EventManager(int id)
    {
        m_Event = nullptr;
        m_Id = id;
    }

    HANDLE GetOrCreateEvent()
    {
        // Here a lock is missing to guard against the data race with m_Event. When multiple threads
        // enter the loop and still see a null event but one or more threads are already creating the event we are leaking 
        // handles which are never cleaned up
        if (m_Event == nullptr)
        {
            m_Event = CreateEvent(m_Id);
        }

        return m_Event;
    }

    ~EventManager()
    {
        if (m_Event != nullptr)
        {
            BOOL lret = ::CloseHandle(m_Event);
            m_Event = nullptr;
        }
    }

private:
    HANDLE CreateEvent(int id)
    {
        std::wstring name(L"SignalEvent_");
        name += std::to_wstring(id);
        HANDLE lret = INVALID_HANDLE_VALUE;

        SECURITY_ATTRIBUTES sa;
        ::ZeroMemory(&sa, sizeof(sa));
        sa.nLength = sizeof(sa);
        sa.bInheritHandle = TRUE;

        lret = ::CreateEvent(&sa, TRUE, FALSE, name.c_str());

        if (lret == nullptr)
        {
            throw std::exception("CreateEvent did fail.");
        }

        return lret;
    }

private:

    HANDLE m_Event;
    int m_Id;
};


class EventConsumer
{
public:
    EventConsumer()
    {
    }

    void GetAndSignal(EventManager &mManager)
    {
        HANDLE hEvent = mManager.GetOrCreateEvent();
        ::SetEvent(hEvent);
    }
};

int main()
{
    print("Start");

    int Run = 0;

    for (int i = 0; i < 100; i++)
    {
        EventManager manager(i);
        auto lambda = [=, &manager]()
            {
                EventConsumer consumer;
                consumer.GetAndSignal(manager);
            };

        std::thread th1(lambda);
        std::thread th2(lambda);
        std::thread th3(lambda);
        std::thread th4(lambda);

        SECURITY_ATTRIBUTES sa;
        ZeroMemory(&sa, sizeof(SECURITY_ATTRIBUTES));
        sa.nLength = sizeof(SECURITY_ATTRIBUTES);

        STARTUPINFO si;
        PROCESS_INFORMATION pi;

        ZeroMemory(&si, sizeof(si));
        si.cb = sizeof(si);
        ZeroMemory(&pi, sizeof(pi));

        if (i == 99)  // Leak handles into child process which is running longer than our application to demonstrate potential races 
        {
            LPTSTR szCmdline = _wcsdup(L"cmd /C for /L %i in (1,1,10000) do echo %i");
            BOOL lret = ::CreateProcess(nullptr, szCmdline, &sa, nullptr, TRUE, CREATE_NO_WINDOW, nullptr, nullptr, &si, &pi);
            if (lret == FALSE)
            {
                formatLine("CreateProcess did fail with error code: %d",::GetLastError());
                break;
            }
        }

        th1.join();
        th2.join();
        th3.join();
        th4.join();
    }

    return 0;
}
