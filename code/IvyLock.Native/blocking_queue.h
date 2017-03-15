#pragma once
#include "blocking_queue.h"
#include <memory>

using namespace std;

namespace IvyLock {
	namespace Native {
		class blocking_queue
		{
		private:
			struct blocking_queue_native;
			unique_ptr<blocking_queue_native> impl = make_unique<blocking_queue_native>();
		};
	}
}