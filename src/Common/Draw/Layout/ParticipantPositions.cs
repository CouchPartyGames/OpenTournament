using System.Collections.Frozen;
using System.Numerics;

namespace OpenTournament.Common.Draw.Layout;

public sealed class InvalidSeedingSizeException(string message) : Exception(message);

public sealed record VersusMatch(int FirstParticipant, int SecondParticipant);


// <summary>
// Get Player Seeding/Positions in the Tournament's First Round depending on player/draw size
// </summary>
public sealed class ParticipantPositions
{
	private readonly DrawSize _drawSize;
	private readonly List<VersusMatch> _matches;

	public ParticipantPositions(DrawSize drawSize)
	{
		_drawSize = drawSize;
		_matches = _drawSize.Value switch
		{
			 DrawSize.Size.Size2 => GetDrawSize2(),
			 DrawSize.Size.Size4 => GetDrawSize4(),
			 DrawSize.Size.Size8 => GetDrawSize8(),
			 DrawSize.Size.Size16 => GetDrawSize16(),
			 DrawSize.Size.Size32 => GetDrawSize32(),
			 DrawSize.Size.Size64 => GetDrawSize64(),
			 DrawSize.Size.Size128 => GetDrawSize128(),
			 _ => throw new InvalidSeedingSizeException( $"Unable to seed matches for size: ${_drawSize.Value}")
		};
	}

	public DrawSize DrawSize
	{
		get { return _drawSize; }
	}

	public List<VersusMatch> Matches
	{
		get
		{
			return _matches;
		}
	}
    
    
    List<VersusMatch> GetDrawSize2() {
    	return new(){
    		new VersusMatch(1, 2)
    	};
	}

	List<VersusMatch> GetDrawSize4() {
    	return new() {
        	new(1, 4),
        	new(3, 2)
    	};
	}

	List<VersusMatch> GetDrawSize8() {
    	return new() {
		    // 1st Half
			new(1, 8),
			new(6, 3),
			// 2nd Half
			new(4, 5),
			new(7, 2),
		};
	}

	List<VersusMatch> GetDrawSize16() {
    	return new() {
		    // 1st Half
			new(1, 16),
			new(9, 8),
			new(4, 13),
			new(5, 12),
			// 2nd Half
			new(3, 14),
			new(11,6),
			new(7,10),
			new(2,15)
		};
	}

	List<VersusMatch> GetDrawSize32() {
   		return new() {
			// 1st Half
			new(1, 32),
			new(16, 17),
			new(9, 24),
			new(8, 25),
			// section
			new(4, 29),
			new(13, 20),
			new(12, 21),
			new(5, 28),
			// 2nd Half
			new(2,31),
			new(15,18),
			new(10,23),
			new(7,26),
			// section
			new(3,30),
			new(14,19),
			new(11,22),
			new(6,27)
		};
	}

	List<VersusMatch> GetDrawSize64() {
		return new() {
			// 1st Bracket
			new(1,64),
			new(32,33),
			new(17,48),
			new(16,49),
			new(9,56),
			new(24,41),
			new(25,40),
			new(8,57),
			// 2nd Bracket
			new(4,61),
			new(29,36),
			new(20,45),
			new(13,52),
			new(12,53),
			new(21,44),
			new(28,37),
			new(5,60),
			// 3rd Bracket
			new(2,63),
			new(31,34),
			new(18,47),
			new(15,50),
			new(10,55),
			new(23,42),
			new(26,39),
			new(7,58),
			// 4th Bracket
			new(3,62),
			new(30,35),
			new(19,46),
			new(14,51),
			new(11,54),
			new(22,43),
			new(27,38),
			new(6,59)
		};
	}

	List<VersusMatch> GetDrawSize128() {
    	return new() {
			// 1st Bracket
			new(1,128),
			new(64,65),
			new(32,97),
			new(33,96),
			new(16,113),
			new(49,80),
			new(17,112),
			new(48,81),
			new(8,121),
			new(57,72),
			new(25,104),
			new(40,89),
			new(9,120),
			new(56,73),
			new(24,105),
			new(41,88),
			// 2nd Bracket
			new(4,125),
			new(61,68),
			new(29,100),
			new(36,93),
			new(13,116),
			new(52,77),
			new(20,109),
			new(45,84),
			new(5,124),
			new(60,69),
			new(28,101),
			new(37,92),
			new(12,117),
			new(53,76),
			new(21,108),
			new(44,85),
			// 3rd Bracket
			new(2,127),
			new(63,66),
			new(31,98),
			new(34,95),
			new(15,114),
			new(50,79),
			new(18,111),
			new(47,82),
			new(7,122),
			new(58,71),
			new(26,103),
			new(39,90),
			new(10,119),
			new(55,74),
			new(23,106),
			new(42,87),
			// 4th Bracket
			new(3,126),
			new(62,67),
			new(30,99),
			new(35,94),
			new(14,115),
			new(51,78),
			new(19,110),
			new(46,83),
			new(6,123),
			new(59,70),
			new(27,102),
			new(38,91),
			new(11,118),
			new(54,75),
			new(22,107),
			new(43,86)
	    };
	}
}