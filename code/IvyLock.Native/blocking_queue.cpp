// #include "Stdafx.h"
#include "blocking_queue.h"
#include "GlobalHook.h"

#include <condition_variable>
#include <mutex>
#include <thread>
#include <iostream>
#include <queue>
#include <chrono>
#include <cassert>

using namespace std;
using namespace IvyLock::Native::ARCH;

inline struct IvyLock::Native::blocking_queue::blocking_queue_native {
private:
	queue<HookCallbackInfoNative> queue_;
	mutex mutex_;
	condition_variable not_empty_cond_;
	condition_variable not_full_cond_;

public:
	blocking_queue_native() {
		// empty
	}

	void put(const HookCallbackInfoNative& t) {
		unique_lock<mutex> lock(mutex_);
		queue_.push(t);
		not_empty_cond_.notify_one();
		assert(!queue_.empty());
	}

	HookCallbackInfoNative take() {
		unique_lock<mutex> lock(mutex_);
		while (queue_.empty()) {
			not_empty_cond_.wait(lock, [&]() { return !queue_.empty(); });
		}
		HookCallbackInfoNative t = queue_.front();
		queue_.pop();
		not_full_cond_.notify_one();
		assert(queue_.size() < capacity_);
		return t;
	}

	int size() const {
		return queue_.size();
	}

	virtual ~blocking_queue_native() {
		// empty
	}
};
