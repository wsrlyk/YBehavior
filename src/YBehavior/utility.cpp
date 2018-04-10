#include "YBehavior/utility.h"
#include <vector>
#include "YBehavior/types.h"
#include "YBehavior/shareddataex.h"

namespace YBehavior
{

	const YBehavior::STRING Utility::StringEmpty("");

	void Utility::SplitString(const STRING& s, std::vector<STRING>& output, CHAR c, int count)
	{
		int counter = 0;
		output.clear();
		STRING::size_type pos1, pos2;
		pos2 = s.find(c);
		pos1 = 0;
		while(STRING::npos != pos2)
		{
			output.push_back(s.substr(pos1, pos2-pos1));
			++counter;

			pos1 = pos2 + 1;

			if (count > 0 && counter >= count)
				break;

			pos2 = s.find(c, pos1);
		}
		if(pos1 != s.length())
			output.push_back(s.substr(pos1));
	}


	void Utility::CreateVector3(const std::vector<STRING>& data, Vector3& vector3)
	{
		if (data.size() < 3)
		{
			if (data.size() > 0)
				vector3.x = static_cast<float>(strtod(data[0].c_str(), 0));
			else
				vector3.x = 0;
			if (data.size() > 1)
				vector3.y = static_cast<float>(strtod(data[1].c_str(), 0));
			else
				vector3.y = 0;
			if (data.size() > 2)
				vector3.z = static_cast<float>(strtod(data[2].c_str(), 0));
			else
				vector3.z = 0;
			return;
		}
		vector3.x = static_cast<float>(strtod(data[0].c_str(), 0));
		vector3.y = static_cast<float>(strtod(data[1].c_str(), 0));
		vector3.z = static_cast<float>(strtod(data[2].c_str(), 0));
	}

	YBehavior::Vector3 Utility::CreateVector3(const std::vector<STRING>& data)
	{
		Vector3 vec;
		CreateVector3(data, vec);
		return vec;
	}
}
